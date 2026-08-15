using Markstash.Domain.Preferences;

namespace Markstash.App.ViewModels;

public sealed record ThemeOptionViewModel(
    ThemePreference Value,
    string DisplayName);
