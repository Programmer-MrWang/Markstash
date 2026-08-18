using Markstash.Backend.Configuration;
using Markstash.Backend.Diagnostics;
using Markstash.Backend.Runtime;
using Markstash.Backend.Services;
using Markstash.Contracts;
using Markstash.Contracts.App;
using Markstash.Contracts.Diagnostics;
using Markstash.Contracts.Health;
using Microsoft.Extensions.Options;

namespace Markstash.Backend.Api;

internal static class ApiEndpoints
{
    private const string SharedResourcesApiBasePath = "/api/v1/resources";

    public static IEndpointRouteBuilder MapMarkstashApi(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        var api = endpoints
            .MapGroup("/api/v1")
            .WithGroupName(ApiContractVersion.OpenApiDocumentName);

        api.MapGet("/health", GetHealth)
            .WithName("GetHealth")
            .WithTags("Health")
            .WithSummary("Reports whether the backend is ready to serve requests.")
            .Produces<HealthResponse>();

        api.MapGet("/app/bootstrap", GetBootstrap)
            .WithName("GetAppBootstrap")
            .WithTags("App")
            .WithSummary("Returns server metadata and negotiated API capabilities.")
            .Produces<AppBootstrapResponse>();

        api.MapGet("/diagnostics/logs", GetDiagnosticLogs)
            .WithName("GetDiagnosticLogs")
            .WithTags("Diagnostics")
            .Produces<DiagnosticLogsResponse>()
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound);

        api.MapPost("/diagnostics/bundle", CreateDiagnosticBundleAsync)
            .WithName("CreateDiagnosticBundle")
            .WithTags("Diagnostics")
            .WithSummary("Creates and downloads a point-in-time server diagnostics archive.")
            .Produces(StatusCodes.Status200OK, contentType: "application/zip")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static HealthResponse GetHealth(
        IOptions<BackendOptions> options,
        BackendRuntimeMetadata metadata,
        ServerRuntimeState runtime,
        TimeProvider timeProvider)
    {
        var checkedAtUtc = timeProvider.GetUtcNow();
        return new HealthResponse(
            "healthy",
            options.Value.ServiceName,
            metadata.Version,
            ApiContractVersion.Current,
            checkedAtUtc,
            runtime.StartedAtUtc,
            GetUptimeSeconds(runtime.StartedAtUtc, checkedAtUtc));
    }

    private static AppBootstrapResponse GetBootstrap(
        IOptions<BackendOptions> options,
        BackendRuntimeMetadata metadata,
        ServerRuntimeState runtime,
        TimeProvider timeProvider)
    {
        var generatedAtUtc = timeProvider.GetUtcNow();
        var backendOptions = options.Value;
        return new AppBootstrapResponse(
            ApiContractVersion.Current,
            new ServiceDescriptorDto(
                backendOptions.ServiceName,
                metadata.Version,
                ApiContractVersion.Current),
            new ServerRuntimeDto(
                runtime.OperatingSystem,
                runtime.Architecture,
                runtime.Framework,
                runtime.ProcessId,
                runtime.StartedAtUtc,
                GetUptimeSeconds(runtime.StartedAtUtc, generatedAtUtc)),
            new BackendCapabilitiesDto(
                new DiagnosticsCapabilitiesDto(
                    backendOptions.ExposeDiagnostics,
                    backendOptions.ExposeDiagnostics,
                    backendOptions.MaximumLogEntries),
                new SharedResourceCapabilitiesDto(
                    IsAvailable: false,
                    CanRead: false,
                    CanWrite: false,
                    SharedResourcesApiBasePath)),
            generatedAtUtc);
    }

    private static IResult GetDiagnosticLogs(
        int? limit,
        IServerDiagnosticsService diagnostics,
        IOptions<BackendOptions> options,
        TimeProvider timeProvider,
        HttpContext httpContext)
    {
        var backendOptions = options.Value;
        if (!backendOptions.ExposeDiagnostics)
        {
            return DiagnosticsDisabled(httpContext);
        }

        var requestedLimit = limit ?? Math.Min(200, backendOptions.MaximumLogEntries);
        if (requestedLimit < 1 || requestedLimit > backendOptions.MaximumLogEntries)
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["limit"] =
                    [
                        $"The limit must be between 1 and {backendOptions.MaximumLogEntries}.",
                    ],
                },
                type: "/problems/validation",
                title: "The request is invalid.",
                extensions: CreateExtensions(httpContext, "validation_failed"));
        }

        var entries = diagnostics
            .ReadLatest(requestedLimit)
            .Select(entry => new DiagnosticLogEntryDto(
                entry.TimestampUtc,
                entry.Level.ToString(),
                entry.Category,
                entry.EventId,
                entry.Message,
                entry.Exception))
            .ToArray();
        return Results.Ok(new DiagnosticLogsResponse(
            requestedLimit,
            entries.Length,
            timeProvider.GetUtcNow(),
            entries));
    }

    private static async Task<IResult> CreateDiagnosticBundleAsync(
        IServerDiagnosticsService diagnostics,
        IOptions<BackendOptions> options,
        HttpContext httpContext,
        CancellationToken cancellationToken)
    {
        if (!options.Value.ExposeDiagnostics)
        {
            return DiagnosticsDisabled(httpContext);
        }

        var archivePath = await diagnostics
            .CreateBundleAsync(cancellationToken)
            .ConfigureAwait(false);
        var fullPath = Path.GetFullPath(archivePath);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "The diagnostics service did not create the requested archive.",
                fullPath);
        }

        var stream = new FileStream(
            fullPath,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read | FileShare.Delete,
            bufferSize: 64 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose);
        return Results.File(
            stream,
            contentType: "application/zip",
            fileDownloadName: Path.GetFileName(fullPath),
            enableRangeProcessing: false);
    }

    private static IResult DiagnosticsDisabled(HttpContext httpContext) =>
        Results.Problem(
            statusCode: StatusCodes.Status404NotFound,
            type: "/problems/diagnostics-disabled",
            title: "Diagnostics are not available.",
            extensions: CreateExtensions(httpContext, "diagnostics_disabled"));

    private static double GetUptimeSeconds(
        DateTimeOffset startedAtUtc,
        DateTimeOffset currentUtc) =>
        Math.Max(0, (currentUtc - startedAtUtc).TotalSeconds);

    private static Dictionary<string, object?> CreateExtensions(
        HttpContext httpContext,
        string code) =>
        new()
        {
            ["code"] = code,
            ["traceId"] = httpContext.TraceIdentifier,
            ["apiVersion"] = ApiContractVersion.Current,
        };
}
