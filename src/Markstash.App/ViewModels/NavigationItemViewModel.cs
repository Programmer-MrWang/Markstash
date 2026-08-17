using FluentAvalonia.UI.Controls;

namespace Markstash.App.ViewModels;

public sealed record NavigationItemViewModel(
    string RouteId,
    string Title,
    FASymbol Icon);
