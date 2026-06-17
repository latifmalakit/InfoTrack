using InfoTrack.Domain.Locations;
using InfoTrack.Domain.Solicitors;

namespace InfoTrack.Domain.SearchRuns;

public sealed record SearchRun(
    Guid Id,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    IReadOnlyList<string> Locations,
    IReadOnlyList<SolicitorListing> Listings,
    IReadOnlyList<LocationSearchFailure> Failures);
