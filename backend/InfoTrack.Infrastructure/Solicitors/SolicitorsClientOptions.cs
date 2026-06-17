namespace InfoTrack.Infrastructure.Solicitors;

public sealed class SolicitorsClientOptions
{
    public const string SectionName = "SolicitorsClient";

    public string BaseAddress { get; init; } = "https://www.solicitors.com";

    public string UserAgent { get; init; } =
        "Mozilla/5.0 (compatible; InfoTrackSolicitorScraper/1.0; +https://github.com/latifmalakit/InfoTrack)";

    public int TimeoutSeconds { get; init; } = 30;

    public Uri GetBaseAddress()
    {
        return Uri.TryCreate(BaseAddress, UriKind.Absolute, out var uri) &&
               (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri
            : throw new InvalidOperationException(
                $"{SectionName}:{nameof(BaseAddress)} must be an absolute HTTP or HTTPS URI.");
    }

    public void Validate()
    {
        _ = GetBaseAddress();

        if (string.IsNullOrWhiteSpace(UserAgent))
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(UserAgent)} is required.");
        }

        if (TimeoutSeconds <= 0)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(TimeoutSeconds)} must be greater than zero.");
        }
    }
}
