using System.Text.Json;
using System.Text.Json.Serialization;
using Markstash.Backend.Configuration;
using Markstash.Backend.Diagnostics;
using Markstash.Backend.Runtime;
using Markstash.Backend.Services;
using Markstash.Contracts;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Markstash.Backend;

public static class DependencyInjection
{
    public static IServiceCollection AddMarkstashBackend(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services
            .AddOptions<BackendOptions>()
            .Bind(configuration.GetSection(BackendOptions.SectionName))
            .Validate(
                options => !string.IsNullOrWhiteSpace(options.ServiceName),
                "The backend service name must not be empty.")
            .Validate(
                options => options.MaximumLogEntries is >= 1 and <= 2000,
                "MaximumLogEntries must be between 1 and 2000.")
            .Validate(
                options => options.DiagnosticBufferCapacity is >= 1 and <= 10000,
                "DiagnosticBufferCapacity must be between 1 and 10000.")
            .Validate(
                options => options.DiagnosticBufferCapacity >= options.MaximumLogEntries,
                "DiagnosticBufferCapacity must not be smaller than MaximumLogEntries.")
            .ValidateOnStart();

        services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] =
                    context.HttpContext.TraceIdentifier;
                context.ProblemDetails.Extensions["apiVersion"] =
                    ApiContractVersion.Current;
            };
        });
        services.AddOpenApi(ApiContractVersion.OpenApiDocumentName);
        services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter(
                    JsonNamingPolicy.CamelCase,
                    allowIntegerValues: false));
        });

        services.TryAddSingleton(TimeProvider.System);
        services.AddSingleton<BackendRuntimeMetadata>();
        services.AddSingleton<ServerRuntimeState>();
        services.AddSingleton<ServerLogBuffer>();
        services.AddSingleton<ServerLogProvider>();
        services.AddSingleton<ILoggerProvider>(provider =>
            provider.GetRequiredService<ServerLogProvider>());
        services.AddSingleton<IServerDiagnosticsService, ServerDiagnosticsService>();
        return services;
    }
}
