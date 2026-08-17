using System.Text.Json;

namespace Markstash.App.Diagnostics;

public static class BootstrapCrashReporter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
    };

    public static void TryWrite(Exception exception, string source)
    {
        string? temporaryPath = null;
        try
        {
            var localData = Environment.GetFolderPath(
                Environment.SpecialFolder.LocalApplicationData);
            var root = string.IsNullOrWhiteSpace(localData)
                ? Path.Combine(Path.GetTempPath(), "Markstash")
                : Path.Combine(localData, "Markstash");
            var directory = Path.Combine(root, "Emergency");
            Directory.CreateDirectory(directory);

            var path = Path.Combine(
                directory,
                $"bootstrap-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmssfff}-{Environment.ProcessId}-{Guid.NewGuid():N}.json");
            temporaryPath = path + ".tmp";
            var report = new
            {
                timestampUtc = DateTimeOffset.UtcNow,
                source,
                processId = Environment.ProcessId,
                exception = Sanitize(exception),
            };
            using (var stream = new FileStream(
                       temporaryPath,
                       FileMode.CreateNew,
                       FileAccess.Write,
                       FileShare.None,
                       bufferSize: 4096,
                       FileOptions.WriteThrough))
            using (var writer = new StreamWriter(stream))
            {
                writer.Write(JsonSerializer.Serialize(report, SerializerOptions));
                writer.Flush();
                stream.Flush(flushToDisk: true);
            }

            File.Move(temporaryPath, path, overwrite: true);
        }
        catch
        {
        }
        finally
        {
            try
            {
                if (temporaryPath is not null && File.Exists(temporaryPath))
                {
                    File.Delete(temporaryPath);
                }
            }
            catch
            {
            }
        }
    }

    private static string Sanitize(Exception exception)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return string.IsNullOrWhiteSpace(userProfile)
            ? exception.ToString()
            : exception.ToString().Replace(userProfile, "~", StringComparison.OrdinalIgnoreCase);
    }
}
