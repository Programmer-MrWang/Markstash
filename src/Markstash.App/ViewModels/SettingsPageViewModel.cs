using Markstash.Application.Abstractions;
using Markstash.Application.Preferences;
using Markstash.Domain.Preferences;

namespace Markstash.App.ViewModels;

public sealed class SettingsPageViewModel : ViewModelBase
{
    private readonly IPreferencesService _preferencesService;
    private ThemeOptionViewModel _selectedTheme;

    public SettingsPageViewModel(
        IPreferencesService preferencesService,
        IAppPaths appPaths)
    {
        _preferencesService = preferencesService;
        DataDirectory = appPaths.DataDirectory;

        ThemeOptions =
        [
            new(ThemePreference.System, "跟随系统"),
            new(ThemePreference.Light, "浅色"),
            new(ThemePreference.Dark, "深色"),
        ];

        _selectedTheme = ThemeOptions.Single(option =>
            option.Value == preferencesService.Current.Theme);
    }

    public string Title => "设置";

    public string DataDirectory { get; }

    public IReadOnlyList<ThemeOptionViewModel> ThemeOptions { get; }

    public ThemeOptionViewModel SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                _preferencesService.SetTheme(value.Value);
            }
        }
    }
}
