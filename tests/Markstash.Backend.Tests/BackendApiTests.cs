using System.IO.Compression;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Markstash.Backend;
using Markstash.Contracts;
using Markstash.Contracts.App;
using Markstash.Contracts.Diagnostics;
using Markstash.Contracts.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Markstash.Backend.Tests;

public sealed class BackendApiTests
{
    private static readonly JsonSerializerOptions JsonOptions =
        new(JsonSerializerDefaults.Web);

    [Fact]
    public async Task HealthReturnsVersionedServerState()
    {
        await using var host = await BackendTestHost.CreateAsync();

        var response = await host.Client.GetFromJsonAsync<HealthResponse>(
            "/api/v1/health",
            JsonOptions);

        Assert.NotNull(response);
        Assert.Equal("healthy", response.Status);
        Assert.Equal("Test Markstash Backend", response.Service);
        Assert.Equal(ApiContractVersion.Current, response.ApiVersion);
        Assert.Equal(BackendTestHost.UtcNow, response.CheckedAtUtc);
        Assert.Equal(BackendTestHost.UtcNow, response.StartedAtUtc);
        Assert.Equal(0, response.UptimeSeconds);
    }

    [Fact]
    public async Task BootstrapAdvertisesNeutralServerCapabilities()
    {
        await using var host = await BackendTestHost.CreateAsync();

        var response = await host.Client.GetFromJsonAsync<AppBootstrapResponse>(
            "/api/v1/app/bootstrap",
            JsonOptions);

        Assert.NotNull(response);
        Assert.Equal(ApiContractVersion.Current, response.ApiVersion);
        Assert.Equal("Test Markstash Backend", response.Service.Name);
        Assert.Equal(ApiContractVersion.Current, response.Service.ApiVersion);
        Assert.Equal(Environment.ProcessId, response.Runtime.ProcessId);
        Assert.True(response.Capabilities.Diagnostics.IsAvailable);
        Assert.True(response.Capabilities.Diagnostics.CanCreateBundle);
        Assert.Equal(25, response.Capabilities.Diagnostics.MaximumLogEntries);
        Assert.False(response.Capabilities.SharedResources.IsAvailable);
        Assert.False(response.Capabilities.SharedResources.CanRead);
        Assert.False(response.Capabilities.SharedResources.CanWrite);
        Assert.Equal(
            "/api/v1/resources",
            response.Capabilities.SharedResources.ApiBasePath);
    }

    [Fact]
    public async Task PlatformLocalPreferencesAreNotExposed()
    {
        await using var host = await BackendTestHost.CreateAsync();

        using var getResponse = await host.Client.GetAsync("/api/v1/preferences");
        using var putResponse = await host.Client.PutAsJsonAsync(
            "/api/v1/preferences",
            new { theme = "dark" });

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, putResponse.StatusCode);
    }

    [Fact]
    public async Task DiagnosticLogsValidateLimitAndReturnServerLogs()
    {
        await using var host = await BackendTestHost.CreateAsync();
        host.Log("Test log entry", eventId: 42);

        using var invalidResponse = await host.Client.GetAsync(
            "/api/v1/diagnostics/logs?limit=26");
        using var invalidBody = JsonDocument.Parse(
            await invalidResponse.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Equal(
            "validation_failed",
            invalidBody.RootElement.GetProperty("code").GetString());

        var response = await host.Client.GetFromJsonAsync<DiagnosticLogsResponse>(
            "/api/v1/diagnostics/logs?limit=25",
            JsonOptions);
        Assert.NotNull(response);
        Assert.Equal(25, response.RequestedLimit);
        var entry = Assert.Single(
            response.Entries,
            item => item.Message.Contains("Test log entry", StringComparison.Ordinal));
        Assert.Equal(42, entry.EventId);
        Assert.Equal("Information", entry.Level);
    }

    [Fact]
    public async Task DiagnosticBundleReturnsServerSnapshotAndDeletesArchive()
    {
        await using var host = await BackendTestHost.CreateAsync();
        host.Log("Bundle log entry", eventId: 84);

        using var response = await host.Client.PostAsync(
            "/api/v1/diagnostics/bundle",
            content: null);
        var bytes = await response.Content.ReadAsByteArrayAsync();
        using var stream = new MemoryStream(bytes);
        using var archive = new ZipArchive(stream, ZipArchiveMode.Read);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/zip", response.Content.Headers.ContentType?.MediaType);
        Assert.NotNull(archive.GetEntry("environment.json"));
        Assert.NotNull(archive.GetEntry("logs.json"));
        Assert.Empty(Directory.EnumerateFiles(host.TemporaryDirectory, "*.zip"));
    }

    [Fact]
    public async Task DisabledDiagnosticsAreHiddenBehindProblemDetails()
    {
        await using var host = await BackendTestHost.CreateAsync(exposeDiagnostics: false);

        using var response = await host.Client.GetAsync("/api/v1/diagnostics/logs");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(
            "diagnostics_disabled",
            body.RootElement.GetProperty("code").GetString());
    }

    [Fact]
    public async Task OpenApiContainsOnlyCurrentVersionedSurface()
    {
        await using var host = await BackendTestHost.CreateAsync();

        using var response = await host.Client.GetAsync("/openapi/v1.json");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var paths = body.RootElement.GetProperty("paths");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(paths.TryGetProperty("/api/v1/health", out _));
        Assert.True(paths.TryGetProperty("/api/v1/app/bootstrap", out _));
        Assert.True(paths.TryGetProperty("/api/v1/diagnostics/logs", out _));
        Assert.False(paths.TryGetProperty("/api/v1/preferences", out _));
    }

    [Fact]
    public async Task UnknownApiRouteReturnsProblemDetails()
    {
        await using var host = await BackendTestHost.CreateAsync();

        using var response = await host.Client.GetAsync("/api/v1/does-not-exist");
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        Assert.Equal(404, body.RootElement.GetProperty("status").GetInt32());
        Assert.Equal(
            ApiContractVersion.Current,
            body.RootElement.GetProperty("apiVersion").GetString());
        Assert.True(body.RootElement.TryGetProperty("traceId", out _));
    }

    private sealed class BackendTestHost : IAsyncDisposable
    {
        public static readonly DateTimeOffset UtcNow =
            new(2026, 8, 17, 12, 30, 0, TimeSpan.Zero);

        private BackendTestHost(
            WebApplication application,
            HttpClient client,
            string temporaryDirectory)
        {
            Application = application;
            Client = client;
            TemporaryDirectory = temporaryDirectory;
        }

        public WebApplication Application { get; }

        public HttpClient Client { get; }

        public string TemporaryDirectory { get; }

        public static async Task<BackendTestHost> CreateAsync(
            bool exposeDiagnostics = true)
        {
            var temporaryDirectory = Path.Combine(
                Path.GetTempPath(),
                $"markstash-backend-test-{Guid.NewGuid():N}");
            var builder = BackendApplication.CreateBuilder([]);
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Markstash:Backend:ServiceName"] = "Test Markstash Backend",
                ["Markstash:Backend:ExposeDiagnostics"] = exposeDiagnostics.ToString(),
                ["Markstash:Backend:MaximumLogEntries"] = "25",
                ["Markstash:Backend:DiagnosticBufferCapacity"] = "100",
                ["Markstash:Backend:TemporaryDirectory"] = temporaryDirectory,
            });
            builder.Services.AddSingleton<TimeProvider>(new FixedTimeProvider(UtcNow));

            var application = BackendApplication.Build(builder);
            await application.StartAsync();
            return new BackendTestHost(
                application,
                application.GetTestClient(),
                temporaryDirectory);
        }

        public void Log(string message, int eventId)
        {
            var logger = Application.Services
                .GetRequiredService<ILogger<BackendApiTests>>();
            logger.Log(
                LogLevel.Information,
                new EventId(eventId),
                message,
                exception: null,
                static (state, _) => state);
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await Application.StopAsync();
            await Application.DisposeAsync();
            try
            {
                if (Directory.Exists(TemporaryDirectory))
                {
                    Directory.Delete(TemporaryDirectory, recursive: true);
                }
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => utcNow;
        }
    }
}
