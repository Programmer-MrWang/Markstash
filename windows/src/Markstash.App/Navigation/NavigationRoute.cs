using FluentAvalonia.UI.Controls;

namespace Markstash.App.Navigation;

public sealed record NavigationRoute(
    string Id,
    string Title,
    FASymbol Icon,
    Type ViewModelType,
    NavigationPlacement Placement = NavigationPlacement.Main);
