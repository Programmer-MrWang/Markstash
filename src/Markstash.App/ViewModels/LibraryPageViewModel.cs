using System.Globalization;
using Markstash.App.Localization;

namespace Markstash.App.ViewModels;

public sealed class LibraryPageViewModel : ViewModelBase
{
    public string Title => AppStrings.NavigationLibrary;

    public int ItemCount => 0;

    public string ItemCountText => AppStrings.LibraryItemCountFormat.Replace(
        "{0}",
        ItemCount.ToString(CultureInfo.CurrentCulture),
        StringComparison.Ordinal);
}
