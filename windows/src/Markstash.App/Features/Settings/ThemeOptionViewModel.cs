using Markstash.Domain.Preferences;

namespace Markstash.App.Features.Settings;

public sealed record ThemeOptionViewModel(
    ThemePreference Value,
    string DisplayName);
