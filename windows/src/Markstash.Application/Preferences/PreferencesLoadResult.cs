using Markstash.Domain.Preferences;

namespace Markstash.Application.Preferences;

public sealed record PreferencesLoadResult(
    UserPreferences Preferences,
    PreferencesLoadStatus Status,
    string? Message = null,
    bool IsWritable = true);
