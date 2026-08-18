using Markstash.App.ViewModels;

namespace Markstash.App.Navigation;

public interface INavigationService
{
    IReadOnlyList<NavigationRoute> Routes { get; }

    NavigationRoute CurrentRoute { get; }

    ViewModelBase CurrentPage { get; }

    bool CanGoBack { get; }

    event EventHandler<NavigationChangedEventArgs>? Navigated;

    bool Navigate(string routeId);

    bool TryNavigate(Uri uri);

    bool GoBack();
}
