using Markstash.App.Hosting;

namespace Markstash.Tests;

public sealed class StartupOptionsTests
{
    [Fact]
    public void ParseRecognizesFoundationOptionsAndLaunchUri()
    {
        var options = AppStartupOptions.Parse(
        [
            "--verbose",
            "--data-dir",
            ".data",
            "markstash://app/settings",
            "--future-option",
        ]);

        Assert.True(options.Verbose);
        Assert.Equal(".data", options.DataDirectory);
        Assert.Equal(new Uri("markstash://app/settings"), options.LaunchUri);
        Assert.Equal(["--future-option"], options.UnrecognizedArguments);
    }
}
