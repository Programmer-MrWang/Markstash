using System.Text.Json;
using System.Text.Json.Serialization;
using Markstash.Application.Abstractions;
using Markstash.Application.Preferences;
using Markstash.Domain.Preferences;

namespace Markstash.Infrastructure.Preferences;

internal sealed class JsonUserPreferencesStore : IUserPreferencesStore
{
    private const int CurrentSchemaVersion = 1;
    private const int CorruptFileRetentionCount = 3;
    private const int WriteLockAttempts = 4;
    private const int WriteLockDelayMilliseconds = 50;

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter(allowIntegerValues: false) },
    };

    private readonly object _gate = new();
    private readonly IAppPaths _paths;

    public JsonUserPreferencesStore(IAppPaths paths)
    {
        _paths = paths;
    }

    public PreferencesLoadResult Load()
    {
        lock (_gate)
        {
            if (!TryEnsureConfigurationDirectory(out var directoryError))
            {
                return new(
                    UserPreferences.Default,
                    PreferencesLoadStatus.ResetAfterFailure,
                    directoryError,
                    IsWritable: false);
            }

            if (File.Exists(_paths.PreferencesFile))
            {
                try
                {
                    var loaded = ReadDocument(_paths.PreferencesFile);
                    if (loaded.IsLegacy)
                    {
                        if (!TrySaveDuringLoad(loaded.Preferences, minimumRevision: 1))
                        {
                            return new(
                                loaded.Preferences,
                                PreferencesLoadStatus.ResetAfterFailure,
                                "The legacy preferences were loaded but could not be migrated.",
                                IsWritable: false);
                        }

                        return new(
                            loaded.Preferences,
                            PreferencesLoadStatus.Migrated,
                            "Migrated the unversioned preferences document to schema version 1.");
                    }

                    return new(loaded.Preferences, PreferencesLoadStatus.Loaded);
                }
                catch (UnsupportedPreferencesVersionException exception)
                {
                    return new(
                        UserPreferences.Default,
                        PreferencesLoadStatus.UnsupportedVersion,
                        exception.Message,
                        IsWritable: false);
                }
                catch (Exception exception) when (IsCorruptDocumentException(exception))
                {
                    if (!TryArchiveCorruptFile(_paths.PreferencesFile))
                    {
                        return new(
                            UserPreferences.Default,
                            PreferencesLoadStatus.ResetAfterFailure,
                            $"Preferences could not be isolated: {exception.Message}",
                            IsWritable: false);
                    }

                    var recovery = TryRecoverFromBackup(
                        $"the primary document failed with {exception.GetType().Name}");
                    if (recovery is not null)
                    {
                        return recovery;
                    }

                    return new(
                        UserPreferences.Default,
                        PreferencesLoadStatus.ResetAfterFailure,
                        $"Preferences could not be read: {exception.Message}");
                }
                catch (Exception exception) when (IsStorageException(exception))
                {
                    return new(
                        UserPreferences.Default,
                        PreferencesLoadStatus.ResetAfterFailure,
                        $"Preferences storage is unavailable: {exception.Message}",
                        IsWritable: false);
                }
            }

            var missingPrimaryRecovery = TryRecoverFromBackup("the primary document was missing");
            if (missingPrimaryRecovery is not null)
            {
                return missingPrimaryRecovery;
            }

            var legacyFile = Path.Combine(_paths.RootDirectory, "preferences.json");
            if (!Path.GetFullPath(legacyFile).Equals(
                    Path.GetFullPath(_paths.PreferencesFile),
                    StringComparison.OrdinalIgnoreCase) &&
                File.Exists(legacyFile))
            {
                try
                {
                    var legacy = ReadDocument(legacyFile).Preferences;
                    if (!TrySaveDuringLoad(legacy, minimumRevision: 1))
                    {
                        return new(
                            legacy,
                            PreferencesLoadStatus.ResetAfterFailure,
                            "Legacy preferences were loaded but could not be moved to the configuration directory.",
                            IsWritable: false);
                    }

                    TryMarkLegacyFileMigrated(legacyFile);
                    return new(
                        legacy,
                        PreferencesLoadStatus.Migrated,
                        "Migrated preferences from the legacy data-directory location.");
                }
                catch (Exception exception) when (IsCorruptDocumentException(exception))
                {
                    var archived = TryArchiveCorruptFile(legacyFile);
                    return new(
                        UserPreferences.Default,
                        PreferencesLoadStatus.ResetAfterFailure,
                        $"Legacy preferences could not be read: {exception.Message}",
                        IsWritable: archived);
                }
                catch (UnsupportedPreferencesVersionException exception)
                {
                    return new(
                        UserPreferences.Default,
                        PreferencesLoadStatus.UnsupportedVersion,
                        $"The legacy preferences file is newer than this application. {exception.Message}",
                        IsWritable: false);
                }
                catch (Exception exception) when (IsStorageException(exception))
                {
                    return new(
                        UserPreferences.Default,
                        PreferencesLoadStatus.ResetAfterFailure,
                        $"Legacy preferences storage is unavailable: {exception.Message}",
                        IsWritable: false);
                }
            }

            return new(UserPreferences.Default, PreferencesLoadStatus.Default);
        }
    }

    public void Save(UserPreferences preferences)
    {
        ArgumentNullException.ThrowIfNull(preferences);

        lock (_gate)
        {
            Directory.CreateDirectory(_paths.ConfigurationDirectory);
            SaveWithWriteLock(preferences);
        }
    }

    private void SaveWithWriteLock(
        UserPreferences preferences,
        long? minimumRevision = null)
    {
        using var writeLock = AcquireWriteLock();
        SaveCore(preferences, minimumRevision);
    }

    private bool TrySaveDuringLoad(
        UserPreferences preferences,
        long? minimumRevision = null)
    {
        try
        {
            SaveWithWriteLock(preferences, minimumRevision);
            return true;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            return false;
        }
    }

    private void SaveCore(
        UserPreferences preferences,
        long? minimumRevision = null)
    {
        Validate(preferences);

        var revision = Math.Max(
            NextRevision(TryReadRevision(_paths.PreferencesFile)),
            minimumRevision ?? 0);
        var document = new PreferencesDocument(
            CurrentSchemaVersion,
            revision,
            DateTimeOffset.UtcNow,
            preferences);
        var temporaryFile = $"{_paths.PreferencesFile}.tmp-{Guid.NewGuid():N}";

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
                JsonSerializer.Serialize(stream, document, SerializerOptions);
                stream.Flush(flushToDisk: true);
            }

            _ = ReadDocument(temporaryFile);

            if (File.Exists(_paths.PreferencesFile))
            {
                try
                {
                    _ = ReadDocument(_paths.PreferencesFile);
                    WriteBackup(_paths.PreferencesFile, GetBackupFile());
                }
                catch (Exception exception) when (IsCorruptDocumentException(exception))
                {
                    if (!TryArchiveCorruptFile(_paths.PreferencesFile))
                    {
                        throw new IOException(
                            "The invalid preferences file could not be isolated.",
                            exception);
                    }
                }
            }

            File.Move(temporaryFile, _paths.PreferencesFile, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }
        }
    }

    private PreferencesLoadResult? TryRecoverFromBackup(string reason)
    {
        var backupFile = GetBackupFile();
        if (!File.Exists(backupFile))
        {
            return null;
        }

        try
        {
            var backup = ReadDocument(backupFile);
            var restored = TrySaveDuringLoad(backup.Preferences, NextRevision(backup.Revision));
            return new(
                backup.Preferences,
                PreferencesLoadStatus.RecoveredFromBackup,
                restored
                    ? $"Recovered preferences from backup because {reason}."
                    : $"Loaded the backup because {reason}, but the primary document could not be restored.",
                IsWritable: restored);
        }
        catch (UnsupportedPreferencesVersionException exception)
        {
            return new(
                UserPreferences.Default,
                PreferencesLoadStatus.UnsupportedVersion,
                $"The preferences backup is newer than this application. {exception.Message}",
                IsWritable: false);
        }
        catch (Exception exception) when (IsCorruptDocumentException(exception))
        {
            if (!TryArchiveCorruptFile(backupFile))
            {
                return new(
                    UserPreferences.Default,
                    PreferencesLoadStatus.ResetAfterFailure,
                    $"The preferences backup could not be isolated: {exception.Message}",
                    IsWritable: false);
            }

            return null;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            return new(
                UserPreferences.Default,
                PreferencesLoadStatus.ResetAfterFailure,
                $"The preferences backup is unavailable: {exception.Message}",
                IsWritable: false);
        }
    }

    private static LoadedPreferences ReadDocument(string path)
    {
        using var stream = File.Open(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);
        using var json = JsonDocument.Parse(stream);
        if (json.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("The preferences document root must be an object.");
        }

        if (!TryGetSingleProperty(json.RootElement, "schemaVersion", out var schemaVersionElement))
        {
            if (!TryGetSingleProperty(json.RootElement, "theme", out _))
            {
                throw new JsonException(
                    "The unversioned preferences document does not contain a theme value.");
            }

            var legacy = json.RootElement.Deserialize<UserPreferences>(SerializerOptions)
                ?? throw new JsonException("The preferences document is empty.");
            Validate(legacy);
            return new(legacy, IsLegacy: true, Revision: 0);
        }

        var schemaVersion = schemaVersionElement.GetInt32();
        if (schemaVersion > CurrentSchemaVersion)
        {
            throw new UnsupportedPreferencesVersionException(schemaVersion);
        }

        if (schemaVersion < 1)
        {
            throw new JsonException($"Unsupported legacy schema version {schemaVersion}.");
        }

        if (!TryGetSingleProperty(json.RootElement, "revision", out _) ||
            !TryGetSingleProperty(json.RootElement, "writtenAtUtc", out _) ||
            !TryGetSingleProperty(json.RootElement, "preferences", out _))
        {
            throw new JsonException(
                "The versioned preferences document is missing required metadata.");
        }

        var document = json.RootElement.Deserialize<PreferencesDocument>(SerializerOptions)
            ?? throw new JsonException("The preferences document is empty.");
        if (document.SchemaVersion != schemaVersion)
        {
            throw new JsonException("The preferences schema version is inconsistent.");
        }
        var preferences = document.Preferences
            ?? throw new JsonException("The preferences payload is null.");
        if (document.Revision < 1 || document.WrittenAtUtc == default)
        {
            throw new JsonException("The preferences metadata is invalid.");
        }

        Validate(preferences);
        return new(preferences, IsLegacy: false, document.Revision);
    }

    private static bool TryGetSingleProperty(
        JsonElement element,
        string propertyName,
        out JsonElement value)
    {
        var found = false;
        value = default;
        foreach (var property in element.EnumerateObject())
        {
            if (property.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                if (found)
                {
                    throw new JsonException(
                        $"The preferences document contains duplicate '{propertyName}' properties.");
                }

                found = true;
                value = property.Value;
            }
        }

        return found;
    }

    private static long TryReadRevision(string path)
    {
        if (!File.Exists(path))
        {
            return 0;
        }

        try
        {
            return ReadDocument(path).Revision;
        }
        catch (Exception exception) when (IsCorruptDocumentException(exception))
        {
            return 0;
        }
    }

    private static long NextRevision(long revision)
    {
        try
        {
            return checked(revision + 1);
        }
        catch (OverflowException exception)
        {
            throw new IOException(
                "The preferences revision has reached its maximum value.",
                exception);
        }
    }

    private static void WriteBackup(string sourceFile, string backupFile)
    {
        if (File.Exists(backupFile))
        {
            try
            {
                // Never let an older binary erase a backup written by a newer schema.
                _ = ReadDocument(backupFile);
            }
            catch (UnsupportedPreferencesVersionException)
            {
                return;
            }
            catch (Exception exception) when (IsCorruptDocumentException(exception))
            {
                // A malformed backup can safely be replaced by the validated primary.
            }
        }

        var temporaryFile = $"{backupFile}.tmp-{Guid.NewGuid():N}";
        try
        {
            using (var source = new FileStream(
                       sourceFile,
                       FileMode.Open,
                       FileAccess.Read,
                       FileShare.ReadWrite | FileShare.Delete,
                       bufferSize: 4096,
                       FileOptions.SequentialScan))
            using (var target = new FileStream(
                       temporaryFile,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       FileOptions.WriteThrough))
            {
                source.CopyTo(target);
                target.Flush(flushToDisk: true);
            }

            File.Move(temporaryFile, backupFile, overwrite: true);
        }
        finally
        {
            if (File.Exists(temporaryFile))
            {
                File.Delete(temporaryFile);
            }
        }
    }

    private bool TryArchiveCorruptFile(string path)
    {
        if (!File.Exists(path))
        {
            return true;
        }

        try
        {
            var corruptDirectory = Path.Combine(_paths.ConfigurationDirectory, "Corrupt");
            Directory.CreateDirectory(corruptDirectory);
            var archivedFile = Path.Combine(
                corruptDirectory,
                $"{Path.GetFileName(path)}.{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss}.{Guid.NewGuid():N}.corrupt");
            File.Move(path, archivedFile, overwrite: false);

            foreach (var staleFile in Directory
                         .EnumerateFiles(corruptDirectory, "*.corrupt")
                         .OrderByDescending(File.GetLastWriteTimeUtc)
                         .Skip(CorruptFileRetentionCount))
            {
                File.Delete(staleFile);
            }

            return true;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            return false;
        }
    }

    private FileStream AcquireWriteLock()
    {
        var lockFile = _paths.PreferencesFile + ".lock";
        IOException? lastException = null;

        for (var attempt = 0; attempt < WriteLockAttempts; attempt++)
        {
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
                Thread.Sleep(WriteLockDelayMilliseconds);
            }
        }

        throw new IOException(
            "Timed out waiting for exclusive access to the preferences store.",
            lastException);
    }

    private static void TryMarkLegacyFileMigrated(string legacyFile)
    {
        try
        {
            File.Move(legacyFile, legacyFile + ".migrated", overwrite: true);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private bool TryEnsureConfigurationDirectory(out string? error)
    {
        try
        {
            Directory.CreateDirectory(_paths.ConfigurationDirectory);
            error = null;
            return true;
        }
        catch (Exception exception) when (IsStorageException(exception))
        {
            error = $"The preferences directory is unavailable: {exception.Message}";
            return false;
        }
    }

    private string GetBackupFile() => _paths.PreferencesFile + ".bak";

    private static bool IsCorruptDocumentException(Exception exception) =>
        exception is JsonException or FormatException or InvalidOperationException;

    private static bool IsStorageException(Exception exception) =>
        exception is IOException or UnauthorizedAccessException;

    private static void Validate(UserPreferences preferences)
    {
        if (!Enum.IsDefined(preferences.Theme))
        {
            throw new JsonException($"Unknown theme preference value {preferences.Theme}.");
        }
    }

    private sealed record PreferencesDocument(
        int SchemaVersion,
        long Revision,
        DateTimeOffset WrittenAtUtc,
        UserPreferences? Preferences);

    private sealed record LoadedPreferences(
        UserPreferences Preferences,
        bool IsLegacy,
        long Revision);

    private sealed class UnsupportedPreferencesVersionException(int schemaVersion)
        : Exception($"Preferences schema version {schemaVersion} is newer than supported version {CurrentSchemaVersion}.");
}
