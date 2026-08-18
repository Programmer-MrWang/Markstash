using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Markstash.ApiClient;

public static class DependencyInjection
{
    public static IServiceCollection AddMarkstashApiClient(
        this IServiceCollection services,
        Action<MarkstashApiClientOptions>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = services.AddOptions<MarkstashApiClientOptions>();
        if (configure is not null)
        {
            options.Configure(configure);
        }

        services.AddHttpClient<IMarkstashApiClient, MarkstashApiClient>((provider, client) =>
        {
            var settings = provider
                .GetRequiredService<IOptions<MarkstashApiClientOptions>>()
                .Value;
            settings.Validate();
            client.BaseAddress = EnsureTrailingSlash(settings.BaseAddress);
            client.Timeout = settings.Timeout;
        });

        return services;
    }

    private static Uri EnsureTrailingSlash(Uri uri)
    {
        var absolute = uri.AbsoluteUri;
        return absolute.EndsWith('/')
            ? uri
            : new Uri(absolute + '/', UriKind.Absolute);
    }
}
