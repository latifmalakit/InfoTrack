using InfoTrack.Domain.Solicitors;

namespace InfoTrack.Application.LocationSearch;

public sealed record LocationSearchResult(
    string Location,
    string SourceUrl,
    IReadOnlyList<SolicitorListing> Listings);
