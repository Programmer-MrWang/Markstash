namespace Markstash.Application.Runtime;

public interface IAppSessionState
{
    bool PreviousSessionEndedUnexpectedly { get; }

    DateTimeOffset StartedAt { get; }
}
