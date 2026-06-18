namespace InfoTrack.Api.Configuration;

public sealed class ClientCorsOptions
{
    public const string SectionName = "Cors";

    public string[] ClientOrigins { get; init; } =
    [
        "http://localhost:5173",
        "http://localhost:8080"
    ];

    public void Validate()
    {
        if (ClientOrigins.Length == 0)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(ClientOrigins)} must contain at least one origin.");
        }

        foreach (var origin in ClientOrigins)
        {
            if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri) ||
                (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps) ||
                !string.IsNullOrEmpty(uri.UserInfo) ||
                !string.Equals(origin, uri.GetLeftPart(UriPartial.Authority), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"{SectionName}:{nameof(ClientOrigins)} contains invalid origin '{origin}'. Use only scheme, host, and optional port.");
            }
        }
    }
}
