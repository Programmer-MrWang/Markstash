using Markstash.Infrastructure.Runtime;

namespace Markstash.Tests;

public sealed class WindowsDesktopIntegrationTests
{
    [Fact]
    public void ShortcutFileCanBeCreatedWithoutTouchingTheDesktop()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var root = Path.Combine(
            Path.GetTempPath(),
            "Markstash.Tests",
            Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var targetPath = Path.Combine(root, "Markstash.Desktop.exe");
            var shortcutPath = Path.Combine(root, "Markstash.lnk");
            File.WriteAllBytes(targetPath, []);

            var result = WindowsDesktopIntegrationService.CreateShortcutFile(
                targetPath,
                shortcutPath,
                root);

            Assert.Equal(shortcutPath, result);
            Assert.True(File.Exists(shortcutPath));
            Assert.True(new FileInfo(shortcutPath).Length > 0);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
