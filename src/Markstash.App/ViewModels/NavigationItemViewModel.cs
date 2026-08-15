using FluentAvalonia.UI.Controls;

namespace Markstash.App.ViewModels;

public sealed record NavigationItemViewModel(
    string Title,
    FASymbol Icon,
    ViewModelBase Page);
