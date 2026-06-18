using InfoTrack.Domain.Locations;
using InfoTrack.Domain.Solicitors;

namespace InfoTrack.Domain.SearchRuns;

public sealed record SearchRun
{
    public SearchRun(
        Guid id,
        DateTimeOffset startedAtUtc,
        DateTimeOffset completedAtUtc,
        IReadOnlyList<string> locations,
        IReadOnlyList<SolicitorListing> listings,
        IReadOnlyList<LocationSearchFailure> failures)
    {
        Id = id;
        StartedAtUtc = startedAtUtc;
        CompletedAtUtc = completedAtUtc;
        Locations = Array.AsReadOnly((locations ?? throw new ArgumentNullException(nameof(locations))).ToArray());
        Listings = Array.AsReadOnly((listings ?? throw new ArgumentNullException(nameof(listings))).ToArray());
        Failures = Array.AsReadOnly((failures ?? throw new ArgumentNullException(nameof(failures))).ToArray());
    }

    public Guid Id { get; }

    public DateTimeOffset StartedAtUtc { get; }

    public DateTimeOffset CompletedAtUtc { get; }

    public IReadOnlyList<string> Locations { get; }

    public IReadOnlyList<SolicitorListing> Listings { get; }

    public IReadOnlyList<LocationSearchFailure> Failures { get; }
}
