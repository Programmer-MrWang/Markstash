using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Markstash.Application.Abstractions;

namespace Markstash.Infrastructure.Runtime;

internal sealed class WindowsDesktopIntegrationService(IAppPaths paths)
    : IDesktopIntegrationService
{
    public bool IsSupported => OperatingSystem.IsWindows();

    public string CreateDesktopShortcut()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw CreatePlatformNotSupportedException();
        }

        return CreateDesktopShortcutCore();
    }

    public void OpenLogsDirectory()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw CreatePlatformNotSupportedException();
        }

        Directory.CreateDirectory(paths.LogsDirectory);
        OpenDirectoryCore(paths.LogsDirectory);
    }

    public void OpenApplicationDirectory()
    {
        if (!OperatingSystem.IsWindows())
        {
            throw CreatePlatformNotSupportedException();
        }

        OpenDirectoryCore(AppContext.BaseDirectory);
    }

    private static PlatformNotSupportedException CreatePlatformNotSupportedException() =>
        new("Desktop integration is only available on Windows.");

    [SupportedOSPlatform("windows")]
    private static string CreateDesktopShortcutCore()
    {
        var desktopDirectory = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktopDirectory))
        {
            throw new DirectoryNotFoundException("The Windows desktop directory is unavailable.");
        }

        Directory.CreateDirectory(desktopDirectory);
        var targetPath = ResolveExecutablePath();
        var shortcutPath = Path.Combine(desktopDirectory, "Markstash.lnk");
        return CreateShortcutFile(targetPath, shortcutPath, AppContext.BaseDirectory);
    }

    [SupportedOSPlatform("windows")]
    internal static string CreateShortcutFile(
        string targetPath,
        string shortcutPath,
        string workingDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(targetPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(shortcutPath);
        ArgumentException.ThrowIfNullOrWhiteSpace(workingDirectory);
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(shortcutPath))!);
        object? shell = null;
        object? shortcut = null;
        try
        {
            var shellType = Type.GetTypeFromProgID("WScript.Shell", throwOnError: true)
                ?? throw new InvalidOperationException("Windows Script Host is unavailable.");
            shell = Activator.CreateInstance(shellType)
                ?? throw new InvalidOperationException("Unable to create Windows Script Host.");
            shortcut = shellType.InvokeMember(
                "CreateShortcut",
                BindingFlags.InvokeMethod,
                binder: null,
                target: shell,
                args: [shortcutPath],
                culture: CultureInfo.InvariantCulture);
            if (shortcut is null)
            {
                throw new InvalidOperationException("Unable to create the shortcut object.");
            }

            var shortcutType = shortcut.GetType();
            SetProperty(shortcutType, shortcut, "TargetPath", targetPath);
            SetProperty(shortcutType, shortcut, "WorkingDirectory", workingDirectory);
            SetProperty(shortcutType, shortcut, "Description", "Markstash");
            SetProperty(shortcutType, shortcut, "IconLocation", $"{targetPath},0");
            shortcutType.InvokeMember(
                "Save",
                BindingFlags.InvokeMethod,
                binder: null,
                target: shortcut,
                args: null,
                culture: CultureInfo.InvariantCulture);
            return shortcutPath;
        }
        finally
        {
            ReleaseComObject(shortcut);
            ReleaseComObject(shell);
        }
    }

    [SupportedOSPlatform("windows")]
    private static void OpenDirectoryCore(string directory)
    {
        var fullPath = Path.GetFullPath(directory);
        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException(fullPath);
        }

        Process.Start(new ProcessStartInfo
        {
            FileName = fullPath,
            UseShellExecute = true,
        });
    }

    [SupportedOSPlatform("windows")]
    private static string ResolveExecutablePath()
    {
        var processPath = Environment.ProcessPath;
        if (!string.IsNullOrWhiteSpace(processPath) &&
            Path.GetFileName(processPath).Equals(
                "Markstash.Desktop.exe",
                StringComparison.OrdinalIgnoreCase))
        {
            return processPath;
        }

        var appHostPath = Path.Combine(AppContext.BaseDirectory, "Markstash.Desktop.exe");
        if (File.Exists(appHostPath))
        {
            return appHostPath;
        }

        throw new FileNotFoundException(
            "Unable to locate Markstash.Desktop.exe.",
            appHostPath);
    }

    private static void SetProperty(
        Type shortcutType,
        object shortcut,
        string propertyName,
        string value)
    {
        shortcutType.InvokeMember(
            propertyName,
            BindingFlags.SetProperty,
            binder: null,
            target: shortcut,
            args: [value],
            culture: CultureInfo.InvariantCulture);
    }

    [SupportedOSPlatform("windows")]
    private static void ReleaseComObject(object? value)
    {
        if (value is not null && Marshal.IsComObject(value))
        {
            Marshal.FinalReleaseComObject(value);
        }
    }
}
