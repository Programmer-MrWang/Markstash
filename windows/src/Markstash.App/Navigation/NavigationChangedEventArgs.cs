using Markstash.App.ViewModels;

namespace Markstash.App.Navigation;

public sealed class NavigationChangedEventArgs(
    NavigationRoute route,
    ViewModelBase page) : EventArgs
{
    public NavigationRoute Route { get; } = route;

    public ViewModelBase Page { get; } = page;
}
