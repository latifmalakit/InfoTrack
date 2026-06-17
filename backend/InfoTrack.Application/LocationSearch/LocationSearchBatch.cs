using InfoTrack.Domain.Locations;
using InfoTrack.Domain.Solicitors;

namespace InfoTrack.Application.LocationSearch;

public sealed record LocationSearchBatch(
    IReadOnlyList<SolicitorListing> Listings,
    IReadOnlyList<LocationSearchFailure> Failures);
