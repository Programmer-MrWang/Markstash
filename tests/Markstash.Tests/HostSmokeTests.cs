using Markstash.App.Hosting;
using Markstash.App.Services;
using Markstash.App.ViewModels;
using Markstash.Application.Diagnostics;
using Markstash.Application.Runtime;
using Microsoft.Extensions.DependencyInjection;

namespace Markstash.Tests;

[Collection(EnvironmentVariableTestGroup.Name)]
public sealed class HostSmokeTests
{
    [Fact]
    public async Task CompositionRootStartsResolvesCoreServicesAndStops()
    {
        var root = Path.Combine(
            Path.GetTempPath(),
            "Markstash.Tests",
            Guid.NewGuid().ToString("N"));
        var previousValue = Environment.GetEnvironmentVariable("MARKSTASH_DATA_DIR");

        try
        {
            Environment.SetEnvironmentVariable("MARKSTASH_DATA_DIR", root);
            using var host = ServiceConfiguration.CreateHost(
                AppStartupOptions.Parse(["markstash://app/settings"]));

            await host.StartAsync(CancellationToken.None);

            var mainViewModel = host.Services.GetRequiredService<MainViewModel>();
            Assert.Equal("settings", mainViewModel.SelectedNavigationItem?.RouteId);
            Assert.DoesNotContain(
                mainViewModel.NavigationItems,
                item => item.RouteId == "settings");
            Assert.Contains(
                mainViewModel.FooterNavigationItems,
                item => item.RouteId == "settings");
            Assert.True(mainViewModel.CanGoBack);
            Assert.False(mainViewModel.IsBackButtonVisible);
            Assert.True(mainViewModel.GoBack());
            Assert.Equal("home", mainViewModel.SelectedNavigationItem?.RouteId);
            Assert.False(mainViewModel.IsBackButtonVisible);
            Assert.NotNull(host.Services.GetRequiredService<IAppDiagnosticsService>());
            Assert.Equal(
                AppLifecycleState.Starting,
                host.Services.GetRequiredService<IAppLifecycle>().State);

            var lifecycle = host.Services.GetRequiredService<IAppLifecycle>();
            lifecycle.StateChanged += ThrowingLifecycleObserver;
            await host.StopAsync(CancellationToken.None);
            Assert.Equal(
                AppLifecycleState.Stopped,
                host.Services.GetRequiredService<IAppLifecycle>().State);
        }
        finally
        {
            Environment.SetEnvironmentVariable("MARKSTASH_DATA_DIR", previousValue);
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static void ThrowingLifecycleObserver(
        object? sender,
        AppLifecycleChangedEventArgs eventArgs) =>
        throw new InvalidOperationException("observer failure");
}
