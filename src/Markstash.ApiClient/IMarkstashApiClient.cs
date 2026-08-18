using Markstash.Contracts.Health;

namespace Markstash.ApiClient;

public interface IMarkstashApiClient
{
    Task<HealthResponse> GetHealthAsync(CancellationToken cancellationToken = default);
}
