using System.Net;
using System.Text;
using Markstash.ApiClient;

namespace Markstash.Tests;

public sealed class ApiClientTests
{
    [Fact]
    public async Task HealthRequestUsesVersionedEndpointAndReadsContract()
    {
        var handler = new StubHandler(request =>
        {
            Assert.Equal(HttpMethod.Get, request.Method);
            Assert.Equal("http://localhost:5080/api/v1/health", request.RequestUri?.AbsoluteUri);
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    """
                    {
                      "status": "healthy",
                      "service": "Markstash Backend",
                      "version": "0.1.0",
                      "apiVersion": "v1",
                      "checkedAtUtc": "2026-08-17T12:00:00Z",
                      "startedAtUtc": "2026-08-17T11:55:00Z",
                      "uptimeSeconds": 300
                    }
                    """,
                    Encoding.UTF8,
                    "application/json"),
            };
        });
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5080/"),
        };
        var client = new MarkstashApiClient(httpClient);

        var response = await client.GetHealthAsync();

        Assert.Equal("healthy", response.Status);
        Assert.Equal("Markstash Backend", response.Service);
        Assert.Equal("v1", response.ApiVersion);
    }

    [Fact]
    public async Task HealthRequestRejectsUnsuccessfulResponse()
    {
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.BadGateway));
        using var httpClient = new HttpClient(handler)
        {
            BaseAddress = new Uri("http://localhost:5080/"),
        };
        var client = new MarkstashApiClient(httpClient);

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetHealthAsync());
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken) =>
            Task.FromResult(responseFactory(request));
    }
}
