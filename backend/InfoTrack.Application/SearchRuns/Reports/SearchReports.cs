using InfoTrack.Domain.Locations;
using InfoTrack.Domain.Solicitors;

namespace InfoTrack.Application.SearchRuns.Reports;

public sealed record SearchRunReport(
    Guid RunId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    SearchSummary Summary,
    IReadOnlyList<LocationReport> ByLocation,
    IReadOnlyList<SolicitorListing> Listings,
    IReadOnlyList<SolicitorListing> NewListings,
    IReadOnlyList<DuplicateFirmReport> DuplicateFirms,
    IReadOnlyList<LocationSearchFailure> Failures);

public sealed record SearchSummary(
    int TotalListings,
    int NewListings,
    int LocationsSearched,
    int FailedLocations,
    int ListingsMissingPhone,
    int ListingsMissingWebsite,
    decimal? AverageRating);

public sealed record LocationReport(
    string Location,
    int ListingCount,
    int NewListingCount,
    int MissingPhoneCount,
    int MissingWebsiteCount,
    decimal? AverageRating);

public sealed record DuplicateFirmReport(
    string Name,
    int Count,
    IReadOnlyList<string> Locations);

public sealed record SearchRunListItem(
    Guid RunId,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset CompletedAtUtc,
    int TotalListings,
    int LocationsSearched,
    int FailedLocations);
