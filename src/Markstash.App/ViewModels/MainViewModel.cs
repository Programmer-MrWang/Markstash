using FluentAvalonia.UI.Controls;

namespace Markstash.App.ViewModels;

public sealed class MainViewModel : ViewModelBase
{
    private NavigationItemViewModel _selectedNavigationItem;
    private ViewModelBase _currentPage;

    public MainViewModel(
        HomePageViewModel homePage,
        LibraryPageViewModel libraryPage,
        SearchPageViewModel searchPage,
        SettingsPageViewModel settingsPage)
    {
        NavigationItems =
        [
            new("概览", FASymbol.Home, homePage),
            new("资源库", FASymbol.Library, libraryPage),
            new("搜索", FASymbol.Find, searchPage),
            new("设置", FASymbol.Settings, settingsPage),
        ];

        _selectedNavigationItem = NavigationItems[0];
        _currentPage = _selectedNavigationItem.Page;
    }

    public string AppTitle => "Markstash";

    public IReadOnlyList<NavigationItemViewModel> NavigationItems { get; }

    public NavigationItemViewModel SelectedNavigationItem
    {
        get => _selectedNavigationItem;
        set
        {
            if (value is not null && SetProperty(ref _selectedNavigationItem, value))
            {
                CurrentPage = value.Page;
            }
        }
    }

    public ViewModelBase CurrentPage
    {
        get => _currentPage;
        private set => SetProperty(ref _currentPage, value);
    }
}
