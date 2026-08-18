using System.IO.Compression;
using System.Text.Json;
using Markstash.Backend.Configuration;
using Markstash.Backend.Runtime;
using Markstash.Backend.Services;
using Markstash.Contracts;
using Microsoft.Extensions.Options;

namespace Markstash.Backend.Diagnostics;

internal sealed class ServerDiagnosticsService(
    ServerLogBuffer logBuffer,
    ServerRuntimeState runtime,
    BackendRuntimeMetadata metadata,
    IOptions<BackendOptions> options,
    TimeProvider timeProvider) : IServerDiagnosticsService
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
    };

    public IReadOnlyList<ServerLogEntry> ReadLatest(int maximumEntries) =>
        logBuffer.ReadLatest(maximumEntries);

    public async Task<string> CreateBundleAsync(
        CancellationToken cancellationToken = default)
    {
        var temporaryDirectory = ResolveTemporaryDirectory(options.Value.TemporaryDirectory);
        Directory.CreateDirectory(temporaryDirectory);
        var archivePath = Path.Combine(
            temporaryDirectory,
            $"markstash-server-diagnostics-{timeProvider.GetUtcNow():yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}.zip");

        try
        {
            await using var file = new FileStream(
                archivePath,
                FileMode.CreateNew,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 64 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan);
            using var archive = new ZipArchive(file, ZipArchiveMode.Create, leaveOpen: true);

            var environmentEntry = archive.CreateEntry("environment.json", CompressionLevel.Fastest);
            await using (var stream = environmentEntry.Open())
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    new ServerEnvironmentSnapshot(
                        ApiContractVersion.Current,
                        metadata.Version,
                        runtime.OperatingSystem,
                        runtime.Architecture,
                        runtime.Framework,
                        runtime.ProcessId,
                        runtime.StartedAtUtc,
                        timeProvider.GetUtcNow()),
                    SerializerOptions,
                    cancellationToken);
            }

            var logsEntry = archive.CreateEntry("logs.json", CompressionLevel.Fastest);
            await using (var stream = logsEntry.Open())
            {
                await JsonSerializer.SerializeAsync(
                    stream,
                    logBuffer.ReadLatest(options.Value.MaximumLogEntries),
                    SerializerOptions,
                    cancellationToken);
            }

            return archivePath;
        }
        catch
        {
            TryDelete(archivePath);
            throw;
        }
    }

    private static string ResolveTemporaryDirectory(string? configuredDirectory) =>
        string.IsNullOrWhiteSpace(configuredDirectory)
            ? Path.Combine(Path.GetTempPath(), "Markstash", "Backend")
            : Path.GetFullPath(configuredDirectory);

    private static void TryDelete(string path)
    {
        try
        {
            File.Delete(path);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    private sealed record ServerEnvironmentSnapshot(
        string ApiVersion,
        string ServiceVersion,
        string OperatingSystem,
        string Architecture,
        string Framework,
        int ProcessId,
        DateTimeOffset StartedAtUtc,
        DateTimeOffset GeneratedAtUtc);
}
