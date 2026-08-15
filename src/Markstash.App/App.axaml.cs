using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Markstash.App.Services;
using Markstash.App.ViewModels;
using Markstash.App.Views;
using Markstash.Application.Preferences;
using Microsoft.Extensions.DependencyInjection;

namespace Markstash.App;

public partial class App : Avalonia.Application
{
    private ServiceProvider? _services;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _services = ServiceConfiguration.Create();

        var preferencesService = _services.GetRequiredService<IPreferencesService>();
        var themeService = _services.GetRequiredService<IThemeService>();
        themeService.Apply(preferencesService.Current.Theme);
        preferencesService.Changed += (_, preferences) => themeService.Apply(preferences.Theme);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow
            {
                DataContext = CreateMainViewModel(),
            };
            desktop.Exit += (_, _) => _services.Dispose();
        }
        else if (ApplicationLifetime is IActivityApplicationLifetime activity)
        {
            activity.MainViewFactory = CreateMainView;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            singleView.MainView = CreateMainView();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private MainView CreateMainView()
    {
        return new MainView
        {
            DataContext = CreateMainViewModel(),
        };
    }

    private MainViewModel CreateMainViewModel()
    {
        return _services?.GetRequiredService<MainViewModel>()
            ?? throw new InvalidOperationException("Application services are not initialized.");
    }
}
