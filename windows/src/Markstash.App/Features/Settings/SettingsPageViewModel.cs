using Markstash.Application.Abstractions;
using Markstash.Application.Preferences;
using Markstash.App.Localization;
using Markstash.App.ViewModels;
using Markstash.Domain.Preferences;

namespace Markstash.App.Features.Settings;

public sealed class SettingsPageViewModel : ViewModelBase
{
    private readonly IPreferencesService _preferencesService;
    private ThemeOptionViewModel _selectedTheme;

    public SettingsPageViewModel(
        IPreferencesService preferencesService,
        IAppPaths appPaths)
    {
        _preferencesService = preferencesService;
        DataDirectory = appPaths.RootDirectory;

        ThemeOptions =
        [
            new(ThemePreference.System, AppStrings.ThemeSystem),
            new(ThemePreference.Light, AppStrings.ThemeLight),
            new(ThemePreference.Dark, AppStrings.ThemeDark),
        ];

        _selectedTheme = ThemeOptions.Single(option =>
            option.Value == preferencesService.Current.Theme);
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822",
        Justification = "Avalonia binding requires an instance property.")]
    public string Title => AppStrings.NavigationSettings;

    public string DataDirectory { get; }

    public IReadOnlyList<ThemeOptionViewModel> ThemeOptions { get; }

    public ThemeOptionViewModel SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                if (!_preferencesService.SetTheme(value.Value))
                {
                    _selectedTheme = ThemeOptions.Single(option =>
                        option.Value == _preferencesService.Current.Theme);
                    OnPropertyChanged();
                }
            }
        }
    }
}
