namespace Markstash.App.ViewModels;

public sealed class SearchPageViewModel : ViewModelBase
{
    private string _query = string.Empty;

    public string Title => "搜索";

    public string Query
    {
        get => _query;
        set => SetProperty(ref _query, value);
    }
}
