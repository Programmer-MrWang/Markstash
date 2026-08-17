using Markstash.Application.Abstractions;
using Markstash.App.Localization;

namespace Markstash.App.ViewModels;

public sealed class HomePageViewModel : ViewModelBase
{
    private readonly IPlatformInfo _platformInfo;

    public HomePageViewModel(IPlatformInfo platformInfo)
    {
        _platformInfo = platformInfo;
    }

    public string Title => AppStrings.NavigationHome;

    public string RuntimeDescription =>
        $"{_platformInfo.Framework} · {_platformInfo.OperatingSystem} · {_platformInfo.Architecture}";
}
