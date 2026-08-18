namespace Markstash.Application.Preferences;

public enum PreferencesLoadStatus
{
    Default,
    Loaded,
    Migrated,
    RecoveredFromBackup,
    ResetAfterFailure,
    UnsupportedVersion,
}
