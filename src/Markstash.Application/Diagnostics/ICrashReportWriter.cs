namespace Markstash.Application.Diagnostics;

public interface ICrashReportWriter
{
    void TryWrite(Exception exception, string source, bool isTerminating);
}
