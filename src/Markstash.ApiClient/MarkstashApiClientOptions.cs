namespace Markstash.ApiClient;

public sealed class MarkstashApiClientOptions
{
    public const string EnvironmentVariable = "MARKSTASH_API_URL";

    public Uri BaseAddress { get; set; } = new("http://localhost:5080/", UriKind.Absolute);

    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(12);

    internal void Validate()
    {
        if (!BaseAddress.IsAbsoluteUri ||
            (BaseAddress.Scheme != Uri.UriSchemeHttp && BaseAddress.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                "The Markstash API base address must be an absolute HTTP or HTTPS URI.");
        }

        if (Timeout <= TimeSpan.Zero || Timeout > TimeSpan.FromMinutes(2))
        {
            throw new InvalidOperationException(
                "The Markstash API timeout must be greater than zero and at most two minutes.");
        }
    }
}
