using Markstash.App.Diagnostics;

namespace Markstash.Desktop;

internal static class EmergencyCrashReporter
{
    public static void TryWrite(Exception exception)
    {
        BootstrapCrashReporter.TryWrite(exception, "Desktop.EntryPoint");
    }
}
