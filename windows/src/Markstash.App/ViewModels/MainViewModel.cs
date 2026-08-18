using Markstash.App.Hosting;
using Markstash.App.Localization;
using Markstash.App.Navigation;
using Microsoft.Extensions.Logging;

namespace Markstash.App.ViewModels;

public sealed partial class MainViewModel : ViewModelBase, IDisposable
{
    private readonly IReadOnlyList<NavigationItemViewModel> _allNavigationItems;
    private readonly INavigationService _navigationService;
    private NavigationItemViewModel? _selectedNavigationItem;
    private ViewModelBase _currentPage;

    public MainViewModel(
        INavigationService navigationService,
        AppStartupOptions startupOptions,
        ILogger<MainViewModel> logger)
    {
        _navigationService = navigationService;
        var navigationItems = navigationService.Routes
            .Where(route => route.Placement != NavigationPlacement.Hidden)
            .Select(route => (
                Route: route,
                Item: new NavigationItemViewModel(route.Id, route.Title, route.Icon)))
            .ToArray();
        _allNavigationItems = navigationItems.Select(pair => pair.Item).ToArray();
        NavigationItems = navigationItems
            .Where(pair => pair.Route.Placement == NavigationPlacement.Main)
            .Select(pair => pair.Item)
            .ToArray();
        FooterNavigationItems = navigationItems
            .Where(pair => pair.Route.Placement == NavigationPlacement.Footer)
            .Select(pair => pair.Item)
            .ToArray();
        _selectedNavigationItem = FindNavigationItem(navigationService.CurrentRoute.Id);
        _currentPage = navigationService.CurrentPage;
        navigationService.Navigated += OnNavigated;

        if (startupOptions.LaunchUri is not null &&
            !navigationService.TryNavigate(startupOptions.LaunchUri))
        {
            LogUnknownLaunchRoute(logger, GetSafeLaunchUri(startupOptions.LaunchUri));
        }
    }

    public string AppTitle => AppStrings.AppName;

    public IReadOnlyList<NavigationItemViewModel> NavigationItems { get; }

    public IReadOnlyList<NavigationItemViewModel> FooterNavigationItems { get; }

    public NavigationItemViewModel? SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (value is not null && value != _selectedNavigationItem)
            {
                _navigationService.Navigate(value.RouteId);
            }
        }
    }

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }

    public bool CanGoBack => _navigationService.CanGoBack;

    public bool IsBackButtonVisible =>
        CanGoBack && _navigationService.CurrentRoute.Placement == NavigationPlacement.Hidden;

    public bool GoBack() => _navigationService.GoBack();

    public void Dispose()
    {
        _navigationService.Navigated -= OnNavigated;
    }

    private void OnNavigated(object? sender, NavigationChangedEventArgs eventArgs)
    {
        SelectedNavigationItemCore = FindNavigationItem(eventArgs.Route.Id);
        CurrentPage = eventArgs.Page;
        OnPropertyChanged(nameof(CanGoBack));
        OnPropertyChanged(nameof(IsBackButtonVisible));
    }

    private NavigationItemViewModel? SelectedNavigationItemCore
    {
        set => SetProperty(ref _selectedNavigationItem, value, nameof(SelectedNavigationItem));
    }

    private NavigationItemViewModel? FindNavigationItem(string routeId) =>
        _allNavigationItems.FirstOrDefault(item =>
            item.RouteId.Equals(routeId, StringComparison.OrdinalIgnoreCase));

    private static string GetSafeLaunchUri(Uri uri) =>
        $"{uri.Scheme}://{uri.Host}{uri.AbsolutePath}";

    [LoggerMessage(
        EventId = 3201,
        Level = LogLevel.Warning,
        Message = "The launch URI does not map to a registered route: {LaunchUri}.")]
    private static partial void LogUnknownLaunchRoute(
        ILogger logger,
        string launchUri);
}
