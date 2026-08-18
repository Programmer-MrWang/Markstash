namespace Markstash.Contracts.App;

public sealed record ServiceDescriptorDto(
    string Name,
    string Version,
    string ApiVersion);

public sealed record ServerRuntimeDto(
    string OperatingSystem,
    string Architecture,
    string Framework,
    int ProcessId,
    DateTimeOffset StartedAtUtc,
    double UptimeSeconds);

public sealed record DiagnosticsCapabilitiesDto(
    bool IsAvailable,
    bool CanCreateBundle,
    int MaximumLogEntries);

public sealed record SharedResourceCapabilitiesDto(
    bool IsAvailable,
    bool CanRead,
    bool CanWrite,
    string ApiBasePath);

public sealed record BackendCapabilitiesDto(
    DiagnosticsCapabilitiesDto Diagnostics,
    SharedResourceCapabilitiesDto SharedResources);

public sealed record AppBootstrapResponse(
    string ApiVersion,
    ServiceDescriptorDto Service,
    ServerRuntimeDto Runtime,
    BackendCapabilitiesDto Capabilities,
    DateTimeOffset GeneratedAtUtc);
