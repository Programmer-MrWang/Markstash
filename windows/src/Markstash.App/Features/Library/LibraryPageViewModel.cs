using System.Globalization;
using Markstash.App.Localization;
using Markstash.App.ViewModels;

namespace Markstash.App.Features.Library;

public sealed class LibraryPageViewModel : ViewModelBase
{
    private readonly int _itemCount;

    public LibraryPageViewModel()
    {
        _itemCount = 0;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822",
        Justification = "Avalonia binding requires an instance property.")]
    public string Title => AppStrings.NavigationLibrary;

    public int ItemCount => _itemCount;

    public string ItemCountText => AppStrings.LibraryItemCountFormat.Replace(
        "{0}",
        ItemCount.ToString(CultureInfo.CurrentCulture),
        StringComparison.Ordinal);
}
