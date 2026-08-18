using Markstash.App.Localization;
using Markstash.App.ViewModels;

namespace Markstash.App.Features.Search;

public sealed class SearchPageViewModel : ViewModelBase
{
    private string _query = string.Empty;

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822",
        Justification = "Avalonia binding requires an instance property.")]
    public string Title => AppStrings.NavigationSearch;

    public string Query
    {
        get => _query;
        set => SetProperty(ref _query, value);
    }
}
