namespace InfoTrack.Infrastructure.Persistence;

public sealed class SearchRunStorageOptions
{
    public const string SectionName = "SearchRunStorage";

    public int MaxStoredRuns { get; init; } = 25;

    public void Validate()
    {
        if (MaxStoredRuns <= 0)
        {
            throw new InvalidOperationException($"{SectionName}:{nameof(MaxStoredRuns)} must be greater than zero.");
        }
    }
}
