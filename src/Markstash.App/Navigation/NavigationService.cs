using Markstash.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace Markstash.App.Navigation;

internal sealed class NavigationService : INavigationService
{
    private readonly Dictionary<string, ViewModelBase> _pageCache =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, NavigationRoute> _routesById;
    private readonly IServiceProvider _services;
    private readonly Stack<string> _backStack = new();

    public NavigationService(
        IServiceProvider services,
        IEnumerable<NavigationRoute> routes)
    {
        _services = services;
        Routes = routes.ToArray();
        if (Routes.Count == 0)
        {
            throw new InvalidOperationException("At least one navigation route is required.");
        }

        _routesById = Routes.ToDictionary(route => route.Id, StringComparer.OrdinalIgnoreCase);
        CurrentRoute = Routes[0];
        CurrentPage = ResolvePage(CurrentRoute);
    }

    public IReadOnlyList<NavigationRoute> Routes { get; }

    public NavigationRoute CurrentRoute { get; private set; }

    public ViewModelBase CurrentPage { get; private set; }

    public bool CanGoBack => _backStack.Count > 0;

    public event EventHandler<NavigationChangedEventArgs>? Navigated;

    public bool Navigate(string routeId)
    {
        if (!_routesById.TryGetValue(routeId, out var route))
        {
            return false;
        }

        return NavigateCore(route, addToHistory: true);
    }

    public bool TryNavigate(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);
        if (!uri.IsAbsoluteUri ||
            !uri.Scheme.Equals("markstash", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var routePath = uri.Host.Equals("app", StringComparison.OrdinalIgnoreCase)
            ? uri.AbsolutePath
            : string.IsNullOrWhiteSpace(uri.Host)
                ? uri.AbsolutePath
                : uri.Host;
        var routeId = routePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        return routeId is not null && Navigate(routeId);
    }

    public bool GoBack()
    {
        if (!_backStack.TryPop(out var routeId) ||
            !_routesById.TryGetValue(routeId, out var route))
        {
            return false;
        }

        return NavigateCore(route, addToHistory: false);
    }

    private bool NavigateCore(NavigationRoute route, bool addToHistory)
    {
        if (route == CurrentRoute)
        {
            return true;
        }

        if (addToHistory)
        {
            _backStack.Push(CurrentRoute.Id);
        }

        CurrentRoute = route;
        CurrentPage = ResolvePage(route);
        var eventArgs = new NavigationChangedEventArgs(route, CurrentPage);
        foreach (var handler in Navigated?.GetInvocationList()
                     .OfType<EventHandler<NavigationChangedEventArgs>>()
                     .ToArray() ?? [])
        {
            try
            {
                handler(this, eventArgs);
            }
            catch (Exception exception)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Unable to notify navigation observer: {exception}");
            }
        }

        return true;
    }

    private ViewModelBase ResolvePage(NavigationRoute route)
    {
        if (_pageCache.TryGetValue(route.Id, out var page))
        {
            return page;
        }

        page = _services.GetRequiredService(route.ViewModelType) as ViewModelBase
            ?? throw new InvalidOperationException(
                $"Navigation route '{route.Id}' does not resolve a view model.");
        _pageCache.Add(route.Id, page);
        return page;
    }
}
