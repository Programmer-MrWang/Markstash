using System.Runtime.InteropServices;

namespace Markstash.App.ViewModels;

public sealed class HomePageViewModel : ViewModelBase
{
    public string Title => "概览";

    public string RuntimeDescription => $".NET {Environment.Version} · {RuntimeInformation.OSDescription}";
}
