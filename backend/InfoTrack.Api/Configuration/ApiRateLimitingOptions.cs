namespace InfoTrack.Api.Configuration;

public sealed class ApiRateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    public int PermitLimit { get; init; } = 60;

    public int WindowSeconds { get; init; } = 60;

    public int QueueLimit { get; init; }

    public void Validate()
    {
        if (PermitLimit <= 0)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(PermitLimit)} must be greater than zero.");
        }

        if (WindowSeconds <= 0)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(WindowSeconds)} must be greater than zero.");
        }

        if (QueueLimit < 0)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(QueueLimit)} cannot be negative.");
        }
    }
}
