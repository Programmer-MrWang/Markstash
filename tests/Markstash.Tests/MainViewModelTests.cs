using FluentAvalonia.UI.Controls;
using Markstash.App.Hosting;
using Markstash.App.Navigation;
using Markstash.App.ViewModels;
using Microsoft.Extensions.Logging.Abstractions;

namespace Markstash.Tests;

public sealed class MainViewModelTests
{
    [Fact]
    public void HiddenRouteShowsBackButtonWhenHistoryExists()
    {
        var homeRoute = new NavigationRoute(
            "home",
            "Home",
            FASymbol.Home,
            typeof(TestPageViewModel));
        var detailRoute = new NavigationRoute(
            "details",
            "Details",
            FASymbol.Library,
            typeof(TestPageViewModel),
            NavigationPlacement.Hidden);
        var navigation = new StubNavigationService(
            [homeRoute, detailRoute],
            detailRoute,
            canGoBack: true);

        using var viewModel = new MainViewModel(
            navigation,
            AppStartupOptions.Default,
            NullLogger<MainViewModel>.Instance);

        Assert.Null(viewModel.SelectedNavigationItem);
        Assert.True(viewModel.CanGoBack);
        Assert.True(viewModel.IsBackButtonVisible);
    }

    private sealed class TestPageViewModel : ViewModelBase;

    private sealed class StubNavigationService(
        IReadOnlyList<NavigationRoute> routes,
        NavigationRoute currentRoute,
        bool canGoBack) : INavigationService
    {
        public IReadOnlyList<NavigationRoute> Routes { get; } = routes;

        public NavigationRoute CurrentRoute { get; } = currentRoute;

        public ViewModelBase CurrentPage { get; } = new TestPageViewModel();

        public bool CanGoBack { get; } = canGoBack;

        public event EventHandler<NavigationChangedEventArgs>? Navigated
        {
            add { }
            remove { }
        }

        public bool Navigate(string routeId) => false;

        public bool TryNavigate(Uri uri) => false;

        public bool GoBack() => false;
    }
}
