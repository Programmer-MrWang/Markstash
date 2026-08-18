using Markstash.Application.Abstractions;
using Markstash.App.Localization;
using Markstash.App.ViewModels;

namespace Markstash.App.Features.Home;

public sealed class HomePageViewModel : ViewModelBase
{
    private readonly IPlatformInfo _platformInfo;

    public HomePageViewModel(IPlatformInfo platformInfo)
    {
        _platformInfo = platformInfo;
    }

    [System.Diagnostics.CodeAnalysis.SuppressMessage(
        "Performance",
        "CA1822",
        Justification = "Avalonia binding requires an instance property.")]
    public string Title => AppStrings.NavigationHome;

    public string RuntimeDescription =>
        $"{_platformInfo.Framework} · {_platformInfo.OperatingSystem} · {_platformInfo.Architecture}";
}
