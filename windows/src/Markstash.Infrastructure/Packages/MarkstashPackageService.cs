using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Markstash.Application.Packages;
using Markstash.Application.Resources;
using Markstash.Domain.Resources;
using Markstash.Infrastructure.Resources;

namespace Markstash.Infrastructure.Packages;

internal sealed class MarkstashPackageService(IResourceRepository repository)
    : IMarkstashPackageService
{
    private const int CurrentPackageVersion = 1;
    private const int CurrentResourceSchemaVersion = 1;
    private const int MaximumArchiveEntries = 128;
    private const int MaximumManifestBytes = 64 * 1024;
    private const int MaximumChecksumsBytes = 1024 * 1024;
    private const int MaximumResourcesBytes = 64 * 1024 * 1024;
    private const string ManifestPath = "manifest.json";
    private const string ResourcesPath = "resources.json";
    private const string ChecksumsPath = "checksums.sha256";

    public async Task<MarkstashPackageExportResult> ExportAsync(
        Stream destination,
        MarkstashPackageExportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(destination);
        if (!destination.CanWrite)
        {
            throw new ArgumentException("The package destination must be writable.", nameof(destination));
        }

        options ??= new();
        var resources = await ResolveExportResourcesAsync(options, cancellationToken);
        var manifest = new PackageManifest(
            Format: "markstash",
            PackageVersion: CurrentPackageVersion,
            CreatedAtUtc: DateTimeOffset.UtcNow,
            ContentMode: "metadata",
            ResourceCount: resources.Count,
            AttachmentsIncluded: false);
        var payload = new PackageResourcesDocument(
            CurrentResourceSchemaVersion,
            resources.Select(ResourceJsonModel.FromDomain).ToArray());
        var manifestBytes = JsonSerializer.SerializeToUtf8Bytes(
            manifest,
            ResourceJsonSerializer.Options);
        var resourceBytes = JsonSerializer.SerializeToUtf8Bytes(
            payload,
            ResourceJsonSerializer.Options);
        var checksumsBytes = Encoding.UTF8.GetBytes(string.Join(
            '\n',
            FormatChecksum(manifestBytes, ManifestPath),
            FormatChecksum(resourceBytes, ResourcesPath)) + "\n");

        using var archive = new ZipArchive(destination, ZipArchiveMode.Create, leaveOpen: true);
        await WriteEntryAsync(archive, ManifestPath, manifestBytes, cancellationToken);
        await WriteEntryAsync(archive, ResourcesPath, resourceBytes, cancellationToken);
        await WriteEntryAsync(archive, ChecksumsPath, checksumsBytes, cancellationToken);
        return new(CurrentPackageVersion, resources.Count);
    }

    public async Task<MarkstashPackageImportResult> ImportAsync(
        Stream source,
        MarkstashPackageImportOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.CanRead)
        {
            throw new ArgumentException("The package source must be readable.", nameof(source));
        }

        options ??= new();
        using var archive = new ZipArchive(source, ZipArchiveMode.Read, leaveOpen: true);
        var entries = ValidateEntries(archive);
        var manifestBytes = await ReadEntryAsync(
            RequireEntry(entries, ManifestPath),
            MaximumManifestBytes,
            cancellationToken);
        var resourceBytes = await ReadEntryAsync(
            RequireEntry(entries, ResourcesPath),
            MaximumResourcesBytes,
            cancellationToken);
        var checksumsBytes = await ReadEntryAsync(
            RequireEntry(entries, ChecksumsPath),
            MaximumChecksumsBytes,
            cancellationToken);

        ValidateChecksums(entries, checksumsBytes, new Dictionary<string, byte[]>(StringComparer.Ordinal)
        {
            [ManifestPath] = manifestBytes,
            [ResourcesPath] = resourceBytes,
        });

        var manifest = Deserialize<PackageManifest>(manifestBytes, ManifestPath);
        ValidateManifest(manifest);
        var resourceDocument = Deserialize<PackageResourcesDocument>(resourceBytes, ResourcesPath);
        if (resourceDocument.SchemaVersion != CurrentResourceSchemaVersion ||
            resourceDocument.Resources is null)
        {
            throw new InvalidDataException("The package resource document has an unsupported schema.");
        }

        var resources = resourceDocument.Resources.Select(ToDomain).ToArray();
        if (resources.Length != manifest.ResourceCount)
        {
            throw new InvalidDataException("The manifest resource count does not match resources.json.");
        }

        if (resources.Select(resource => resource.Id).Distinct().Count() != resources.Length)
        {
            throw new InvalidDataException("The package contains duplicate resource IDs.");
        }

        var result = await repository.UpsertAsync(
            resources,
            options.OverwriteExisting,
            cancellationToken);
        return new(CurrentPackageVersion, result.AddedCount, result.UpdatedCount);
    }

    private async Task<IReadOnlyList<ResourceRecord>> ResolveExportResourcesAsync(
        MarkstashPackageExportOptions options,
        CancellationToken cancellationToken)
    {
        if (options.ResourceIds is { } selectedIds)
        {
            var duplicateId = selectedIds
                .GroupBy(resourceId => resourceId)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicateId is not null)
            {
                throw new ArgumentException(
                    $"Resource '{duplicateId.Key}' was selected more than once.",
                    nameof(options));
            }

            var selected = new List<ResourceRecord>(selectedIds.Count);
            foreach (var resourceId in selectedIds)
            {
                selected.Add(await repository.GetAsync(resourceId, cancellationToken)
                    ?? throw new ResourceNotFoundException(resourceId));
            }

            return selected.OrderBy(resource => resource.Id.Value, StringComparer.Ordinal).ToArray();
        }

        var resources = new List<ResourceRecord>();
        while (true)
        {
            var page = await repository.ListAsync(
                new ResourceQuery(offset: resources.Count, limit: ResourceQuery.MaximumLimit),
                cancellationToken);
            resources.AddRange(page);
            if (page.Count < ResourceQuery.MaximumLimit)
            {
                break;
            }
        }

        return resources.OrderBy(resource => resource.Id.Value, StringComparer.Ordinal).ToArray();
    }

    private static Dictionary<string, ZipArchiveEntry> ValidateEntries(ZipArchive archive)
    {
        if (archive.Entries.Count > MaximumArchiveEntries)
        {
            throw new InvalidDataException("The package contains too many archive entries.");
        }

        var entries = new Dictionary<string, ZipArchiveEntry>(StringComparer.Ordinal);
        foreach (var entry in archive.Entries)
        {
            ValidateEntryPath(entry.FullName);
            if (!entries.TryAdd(entry.FullName, entry))
            {
                throw new InvalidDataException($"The package contains duplicate entry '{entry.FullName}'.");
            }

            var isDirectory = entry.FullName.EndsWith('/');
            var isKnownFile = entry.FullName is ManifestPath or ResourcesPath or ChecksumsPath;
            var isReservedAttachmentDirectory = isDirectory && entry.FullName == "attachments/";
            if (!isKnownFile && !isReservedAttachmentDirectory)
            {
                throw new InvalidDataException(
                    $"Package entry '{entry.FullName}' is not supported by package version {CurrentPackageVersion}.");
            }
        }

        return entries;
    }

    private static void ValidateEntryPath(string entryPath)
    {
        if (string.IsNullOrWhiteSpace(entryPath) ||
            entryPath.StartsWith('/') ||
            entryPath.Contains('\\') ||
            entryPath.Contains(':'))
        {
            throw new InvalidDataException($"Package entry path '{entryPath}' is unsafe.");
        }

        var segments = entryPath.Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (segments.Any(segment => segment is "." or ".."))
        {
            throw new InvalidDataException($"Package entry path '{entryPath}' is unsafe.");
        }
    }

    private static void ValidateChecksums(
        Dictionary<string, ZipArchiveEntry> entries,
        byte[] checksumsBytes,
        Dictionary<string, byte[]> payloads)
    {
        var checksumText = new UTF8Encoding(
            encoderShouldEmitUTF8Identifier: false,
            throwOnInvalidBytes: true).GetString(checksumsBytes);
        var checksums = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var line in checksumText.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var normalizedLine = line.TrimEnd('\r');
            if (normalizedLine.Length < 67 || normalizedLine[64..66] != "  ")
            {
                throw new InvalidDataException("checksums.sha256 contains an invalid line.");
            }

            var hash = normalizedLine[..64];
            var path = normalizedLine[66..];
            if (hash.Any(character => !Uri.IsHexDigit(character)))
            {
                throw new InvalidDataException("checksums.sha256 contains a non-hexadecimal hash.");
            }

            ValidateEntryPath(path);
            if (!checksums.TryAdd(path, hash.ToLowerInvariant()))
            {
                throw new InvalidDataException($"checksums.sha256 contains duplicate path '{path}'.");
            }
        }

        var expectedPaths = entries
            .Where(pair => pair.Key != ChecksumsPath &&
                           !pair.Key.EndsWith('/'))
            .Select(pair => pair.Key)
            .ToHashSet(StringComparer.Ordinal);
        if (!expectedPaths.SetEquals(checksums.Keys) || !expectedPaths.SetEquals(payloads.Keys))
        {
            throw new InvalidDataException("checksums.sha256 does not cover exactly the package payload files.");
        }

        foreach (var (path, payload) in payloads)
        {
            var actual = Convert.ToHexStringLower(SHA256.HashData(payload));
            if (!CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(actual),
                    Encoding.ASCII.GetBytes(checksums[path])))
            {
                throw new InvalidDataException($"Checksum validation failed for '{path}'.");
            }
        }
    }

    private static void ValidateManifest(PackageManifest manifest)
    {
        if (manifest.Format != "markstash" ||
            manifest.PackageVersion != CurrentPackageVersion ||
            manifest.ContentMode != "metadata" ||
            manifest.ResourceCount < 0 ||
            manifest.AttachmentsIncluded ||
            manifest.CreatedAtUtc == default ||
            manifest.CreatedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new InvalidDataException("The package manifest is invalid or unsupported.");
        }
    }

    private static T Deserialize<T>(byte[] payload, string entryPath)
    {
        try
        {
            return JsonSerializer.Deserialize<T>(payload, ResourceJsonSerializer.Options)
                ?? throw new InvalidDataException($"Package entry '{entryPath}' is empty.");
        }
        catch (JsonException exception)
        {
            throw new InvalidDataException($"Package entry '{entryPath}' contains invalid JSON.", exception);
        }
    }

    private static ResourceRecord ToDomain(ResourceJsonModel model)
    {
        try
        {
            return model.ToDomain();
        }
        catch (ArgumentException exception)
        {
            throw new InvalidDataException("The package contains an invalid resource.", exception);
        }
    }

    private static ZipArchiveEntry RequireEntry(
        Dictionary<string, ZipArchiveEntry> entries,
        string entryPath) =>
        entries.TryGetValue(entryPath, out var entry)
            ? entry
            : throw new InvalidDataException($"The package is missing '{entryPath}'.");

    private static async Task<byte[]> ReadEntryAsync(
        ZipArchiveEntry entry,
        int maximumBytes,
        CancellationToken cancellationToken)
    {
        if (entry.Length > maximumBytes)
        {
            throw new InvalidDataException($"Package entry '{entry.FullName}' is too large.");
        }

        await using var source = entry.Open();
        using var destination = new MemoryStream((int)entry.Length);
        var buffer = new byte[81920];
        while (true)
        {
            var read = await source.ReadAsync(buffer, cancellationToken);
            if (read == 0)
            {
                break;
            }

            if (destination.Length + read > maximumBytes)
            {
                throw new InvalidDataException($"Package entry '{entry.FullName}' is too large.");
            }

            await destination.WriteAsync(buffer.AsMemory(0, read), cancellationToken);
        }

        return destination.ToArray();
    }

    private static async Task WriteEntryAsync(
        ZipArchive archive,
        string entryPath,
        byte[] payload,
        CancellationToken cancellationToken)
    {
        var entry = archive.CreateEntry(entryPath, CompressionLevel.Optimal);
        await using var stream = entry.Open();
        await stream.WriteAsync(payload, cancellationToken);
    }

    private static string FormatChecksum(byte[] payload, string entryPath) =>
        $"{Convert.ToHexStringLower(SHA256.HashData(payload))}  {entryPath}";

    private sealed record PackageManifest(
        string Format,
        int PackageVersion,
        DateTimeOffset CreatedAtUtc,
        string ContentMode,
        int ResourceCount,
        bool AttachmentsIncluded);

    private sealed record PackageResourcesDocument(
        int SchemaVersion,
        IReadOnlyList<ResourceJsonModel>? Resources);
}
