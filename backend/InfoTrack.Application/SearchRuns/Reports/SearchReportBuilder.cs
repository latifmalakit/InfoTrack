using InfoTrack.Domain.SearchRuns;
using InfoTrack.Domain.Solicitors;

namespace InfoTrack.Application.SearchRuns.Reports;

public sealed class SearchReportBuilder
{
    public SearchRunReport Build(SearchRun currentRun, SearchRun? previousRun)
    {
        var previousFirmKeys = previousRun?.Listings
            .Select(SolicitorFirmKey.Create)
            .ToHashSet(StringComparer.OrdinalIgnoreCase) ?? [];

        var newListings = currentRun.Listings
            .Where(listing => !previousFirmKeys.Contains(SolicitorFirmKey.Create(listing)))
            .ToList();

        var newKeys = newListings
            .Select(listing => listing.ExternalKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var byLocation = currentRun.Listings
            .GroupBy(listing => listing.Location, StringComparer.OrdinalIgnoreCase)
            .Select(group => new LocationReport(
                group.Key,
                group.Count(),
                group.Count(listing => newKeys.Contains(listing.ExternalKey)),
                group.Count(listing => string.IsNullOrWhiteSpace(listing.ContactDetails.PhoneNumber)),
                group.Count(listing => string.IsNullOrWhiteSpace(listing.ContactDetails.WebsiteUrl)),
                AverageRating(group)))
            .OrderByDescending(report => report.ListingCount)
            .ThenBy(report => report.Location)
            .ToList();

        var duplicateFirms = currentRun.Listings
            .GroupBy(listing => NormalizeName(listing.Name), StringComparer.OrdinalIgnoreCase)
            .Where(group => group.Count() > 1)
            .Select(group => new DuplicateFirmReport(
                group.First().Name,
                group.Count(),
                group.Select(listing => listing.Location)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(location => location)
                    .ToList()))
            .OrderByDescending(report => report.Count)
            .ThenBy(report => report.Name)
            .ToList();

        var summary = new SearchSummary(
            currentRun.Listings.Count,
            newListings.Count,
            currentRun.Locations.Count,
            currentRun.Failures.Count,
            currentRun.Listings.Count(listing => string.IsNullOrWhiteSpace(listing.ContactDetails.PhoneNumber)),
            currentRun.Listings.Count(listing => string.IsNullOrWhiteSpace(listing.ContactDetails.WebsiteUrl)),
            AverageRating(currentRun.Listings));

        return new SearchRunReport(
            currentRun.Id,
            currentRun.StartedAtUtc,
            currentRun.CompletedAtUtc,
            summary,
            byLocation,
            currentRun.Listings,
            newListings,
            duplicateFirms,
            currentRun.Failures);
    }

    private static decimal? AverageRating(IEnumerable<SolicitorListing> listings)
    {
        var scores = listings
            .Select(listing => listing.Rating.Score)
            .Where(score => score.HasValue)
            .Select(score => score!.Value)
            .ToList();

        return scores.Count == 0 ? null : Math.Round(scores.Average(), 2);
    }

    private static string NormalizeName(string name)
    {
        return string.Join(' ', name.Trim().ToLowerInvariant().Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
