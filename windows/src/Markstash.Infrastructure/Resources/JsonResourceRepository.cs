using System.Text.Json;
using Markstash.Application.Abstractions;
using Markstash.Application.Resources;
using Markstash.Domain.Resources;

namespace Markstash.Infrastructure.Resources;

internal sealed class JsonResourceRepository : IResourceRepository, IDisposable
{
    private const int CurrentSchemaVersion = 1;
    private const int WriteLockAttempts = 20;
    private const int WriteLockDelayMilliseconds = 25;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly string _resourceFile;

    public JsonResourceRepository(IAppPaths paths)
    {
        _resourceFile = Path.Combine(paths.DatabaseDirectory, "resources.json");
    }

    public async Task<ResourceRecord?> GetAsync(
        ResourceId resourceId,
        CancellationToken cancellationToken = default)
    {
        EnsureResourceId(resourceId);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return LoadDocument().Resources.FirstOrDefault(resource => resource.Id == resourceId);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<IReadOnlyList<ResourceRecord>> ListAsync(
        ResourceQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            IEnumerable<ResourceRecord> filtered = LoadDocument().Resources;
            if (query.Kinds.Count > 0)
            {
                var kinds = query.Kinds.ToHashSet();
                filtered = filtered.Where(resource => kinds.Contains(resource.Kind));
            }

            if (query.FavoritesOnly)
            {
                filtered = filtered.Where(resource => resource.IsFavorite);
            }

            if (query.Tags.Count > 0)
            {
                filtered = filtered.Where(resource => query.Tags.All(queryTag =>
                    resource.Tags.Contains(queryTag, StringComparer.OrdinalIgnoreCase)));
            }

            if (query.Text is { } text)
            {
                filtered = filtered.Where(resource => MatchesText(resource, text));
            }

            return filtered
                .OrderByDescending(resource => resource.UpdatedAtUtc)
                .ThenBy(resource => resource.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(resource => resource.Id.Value, StringComparer.Ordinal)
                .Skip(query.Offset)
                .Take(query.Limit)
                .ToArray();
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<ResourceBatchWriteResult> UpsertAsync(
        IReadOnlyCollection<ResourceRecord> resources,
        bool overwriteExisting,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(resources);
        if (resources.Count == 0)
        {
            return new(0, 0);
        }

        var incoming = resources.ToArray();
        if (incoming.Any(resource => resource is null))
        {
            throw new ArgumentException("A resource collection cannot contain null entries.", nameof(resources));
        }

        var duplicateIds = incoming
            .GroupBy(resource => resource.Id)
            .Where(group => group.Count() > 1)
            .Select(group => group.Key)
            .ToArray();
        if (duplicateIds.Length > 0)
        {
            throw new ArgumentException(
                "A resource batch cannot contain duplicate IDs.",
                nameof(resources));
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_resourceFile)!);
            using var writeLock = AcquireWriteLock(cancellationToken);
            var document = LoadDocument();
            var byId = document.Resources.ToDictionary(resource => resource.Id);
            var conflicts = incoming
                .Where(resource => byId.ContainsKey(resource.Id))
                .Select(resource => resource.Id)
                .ToArray();
            if (!overwriteExisting && conflicts.Length > 0)
            {
                throw new ResourceConflictException(conflicts);
            }

            var addedCount = 0;
            var updatedCount = 0;
            foreach (var resource in incoming)
            {
                if (byId.TryAdd(resource.Id, resource))
                {
                    addedCount++;
                }
                else
                {
                    byId[resource.Id] = resource;
                    updatedCount++;
                }
            }

            SaveDocument(byId.Values, NextRevision(document.Revision));
            return new(addedCount, updatedCount);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<bool> DeleteAsync(
        ResourceId resourceId,
        CancellationToken cancellationToken = default)
    {
        EnsureResourceId(resourceId);
        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_resourceFile)!);
            using var writeLock = AcquireWriteLock(cancellationToken);
            var document = LoadDocument();
            var remaining = document.Resources
                .Where(resource => resource.Id != resourceId)
                .ToArray();
            if (remaining.Length == document.Resources.Count)
            {
                return false;
            }

            SaveDocument(remaining, NextRevision(document.Revision));
            return true;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose()
    {
        _gate.Dispose();
    }

    private StoredResourceDocument LoadDocument()
    {
        if (!File.Exists(_resourceFile))
        {
            return new(CurrentSchemaVersion, 0, []);
        }

        try
        {
            using var stream = new FileStream(
                _resourceFile,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);
            var stored = JsonSerializer.Deserialize<StoredResourceJsonDocument>(
                    stream,
                    ResourceJsonSerializer.Options)
                ?? throw new InvalidDataException("The resource document is empty.");

            if (stored.SchemaVersion > CurrentSchemaVersion)
            {
                throw new InvalidDataException(
                    $"Resource schema version {stored.SchemaVersion} is newer than supported version {CurrentSchemaVersion}.");
            }

            if (stored.SchemaVersion != CurrentSchemaVersion ||
                stored.Revision < 1 ||
                stored.WrittenAtUtc == default ||
                stored.WrittenAtUtc.Offset != TimeSpan.Zero ||
                stored.Resources is null)
            {
                throw new InvalidDataException("The resource document metadata is invalid.");
            }

            var resources = stored.Resources.Select(ToDomain).ToArray();
            if (resources.Select(resource => resource.Id).Distinct().Count() != resources.Length)
            {
                throw new InvalidDataException("The resource document contains duplicate IDs.");
            }

            return new(stored.SchemaVersion, stored.Revision, resources);
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException("The resource document contains invalid JSON.", exception);
        }
    }

    private void SaveDocument(IEnumerable<ResourceRecord> resources, long revision)
    {
        var ordered = resources
            .OrderBy(resource => resource.Id.Value, StringComparer.Ordinal)
            .Select(ResourceJsonModel.FromDomain)
            .ToArray();
        var document = new StoredResourceJsonDocument(
            CurrentSchemaVersion,
            revision,
            DateTimeOffset.UtcNow,
            ordered);
        var temporaryFile = $"{_resourceFile}.tmp-{Guid.NewGuid():N}";

        try
        {
            using (var stream = new FileStream(
                       temporaryFile,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       FileOptions.WriteThrough))
            {
                JsonSerializer.Serialize(stream, document, ResourceJsonSerializer.Options);
                stream.Flush(flushToDisk: true);
            }

            ValidateTemporaryDocument(temporaryFile, revision, ordered.Length);
            File.Move(temporaryFile, _resourceFile, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }
        }
    }

    private static void ValidateTemporaryDocument(
        string temporaryFile,
        long expectedRevision,
        int expectedResourceCount)
    {
        using var stream = File.OpenRead(temporaryFile);
        var document = JsonSerializer.Deserialize<StoredResourceJsonDocument>(
                stream,
                ResourceJsonSerializer.Options)
            ?? throw new InvalidDataException("The temporary resource document is empty.");
        if (document.SchemaVersion != CurrentSchemaVersion ||
            document.Revision != expectedRevision ||
            document.Resources?.Count != expectedResourceCount)
        {
            throw new InvalidDataException("The temporary resource document failed validation.");
        }
    }

    private FileStream AcquireWriteLock(CancellationToken cancellationToken)
    {
        var lockFile = _resourceFile + ".lock";
        IOException? lastException = null;
        for (var attempt = 0; attempt < WriteLockAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(
                    lockFile,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.None);
            }
            catch (IOException exception)
            {
                lastException = exception;
                if (cancellationToken.WaitHandle.WaitOne(WriteLockDelayMilliseconds))
                {
                    cancellationToken.ThrowIfCancellationRequested();
                }
            }
        }

        throw new IOException(
            "Timed out waiting for exclusive access to the resource store.",
            lastException);
    }

    private static ResourceRecord ToDomain(ResourceJsonModel model)
    {
        try
        {
            return model.ToDomain();
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("The resource document contains an invalid resource.", exception);
        }
    }

    private static bool MatchesText(ResourceRecord resource, string text) =>
        Contains(resource.Title, text) ||
        Contains(resource.Source, text) ||
        Contains(resource.Description, text) ||
        resource.Tags.Any(tag => Contains(tag, text));

    private static bool Contains(string? candidate, string text) =>
        candidate?.Contains(text, StringComparison.OrdinalIgnoreCase) == true;

    private static long NextRevision(long revision)
    {
        try
        {
            return checked(revision + 1);
        }
        catch (OverflowException exception)
        {
            throw new IOException("The resource revision has reached its maximum value.", exception);
        }
    }

    private static void EnsureResourceId(ResourceId resourceId)
    {
        if (resourceId == default)
        {
            throw new ArgumentException("A resource ID is required.", nameof(resourceId));
        }
    }

    private sealed record StoredResourceDocument(
        int SchemaVersion,
        long Revision,
        IReadOnlyList<ResourceRecord> Resources);

    private sealed record StoredResourceJsonDocument(
        int SchemaVersion,
        long Revision,
        DateTimeOffset WrittenAtUtc,
        IReadOnlyList<ResourceJsonModel>? Resources);
}
