using System.Text.Json.Serialization;

namespace InfoTrack.Domain.Solicitors;

public sealed record Rating
{
    public static Rating Empty { get; } = new(null, null);

    [JsonConstructor]
    private Rating(decimal? score, int? reviewCount)
    {
        if (score is < 0m or > 5m)
        {
            throw new ArgumentOutOfRangeException(nameof(score), "Rating score must be between 0 and 5.");
        }

        if (reviewCount is < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(reviewCount), "Review count cannot be negative.");
        }

        Score = score;
        ReviewCount = reviewCount;
    }

    public decimal? Score { get; }

    public int? ReviewCount { get; }

    public static Rating Create(decimal? score, int? reviewCount)
    {
        return score is null && reviewCount is null
            ? Empty
            : new Rating(score, reviewCount);
    }
}
