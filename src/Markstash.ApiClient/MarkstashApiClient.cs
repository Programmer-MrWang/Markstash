using System.Net.Http.Json;
using Markstash.Contracts.Health;

namespace Markstash.ApiClient;

public sealed class MarkstashApiClient(HttpClient httpClient) : IMarkstashApiClient
{
    public async Task<HealthResponse> GetHealthAsync(
        CancellationToken cancellationToken = default)
    {
        using var response = await httpClient
            .GetAsync("api/v1/health", cancellationToken)
            .ConfigureAwait(false);
        response.EnsureSuccessStatusCode();

        return await response.Content
                   .ReadFromJsonAsync<HealthResponse>(cancellationToken)
                   .ConfigureAwait(false)
               ?? throw new InvalidDataException(
                   "The Markstash backend returned an empty health response.");
    }
}
