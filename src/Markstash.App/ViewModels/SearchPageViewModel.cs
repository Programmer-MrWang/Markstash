using Markstash.App.Localization;

namespace Markstash.App.ViewModels;

public sealed class SearchPageViewModel : ViewModelBase
{
    private string _query = string.Empty;

    public string Title => AppStrings.NavigationSearch;

    public string Query
    {
        get => _query;
        set => SetProperty(ref _query, value);
    }
}
