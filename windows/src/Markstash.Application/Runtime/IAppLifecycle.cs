namespace Markstash.Application.Runtime;

public interface IAppLifecycle
{
    AppLifecycleState State { get; }

    CancellationToken StoppingToken { get; }

    event EventHandler<AppLifecycleChangedEventArgs>? StateChanged;
}
