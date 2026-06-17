using InfoTrack.Application.SearchRuns.Reports;
using InfoTrack.Domain.SearchRuns;
using InfoTrack.Domain.Solicitors;

namespace InfoTrack.UnitTests;

public sealed class SearchReportBuilderTests
{
    [Fact]
    public void Build_aggregates_locations_and_marks_new_listings()
    {
        var previous = CreateRun(
            CreateListing("old-key", "Alpha Law", "London", rating: 4.5m, phone: "020", website: "https://alpha.example"));

        var current = CreateRun(
            CreateListing("old-key", "Alpha Law", "London", rating: 4.5m, phone: "020", website: "https://alpha.example"),
            CreateListing("new-key", "Beta Law", "Leeds", rating: 5m, phone: null, website: null),
            CreateListing("dup-key", "Alpha Law", "Leeds", rating: null, phone: "0113", website: null));

        var report = new SearchReportBuilder().Build(current, previous);

        Assert.Equal(3, report.Summary.TotalListings);
        Assert.Equal(2, report.Summary.NewListings);
        Assert.Equal(1, report.Summary.ListingsMissingPhone);
        Assert.Equal(2, report.Summary.ListingsMissingWebsite);
        Assert.Equal(4.75m, report.Summary.AverageRating);
        Assert.Contains(report.ByLocation, location => location.Location == "Leeds" && location.ListingCount == 2);
        Assert.Contains(report.DuplicateFirms, duplicate => duplicate.Name == "Alpha Law" && duplicate.Count == 2);
    }

    [Fact]
    public void Build_compares_new_listings_by_firm_identity_not_office_key()
    {
        const string profileUrl = "https://www.solicitors.com/gamma-law.html";
        var previous = CreateRun(
            CreateListing(
                "gamma-office-one",
                "Gamma Law",
                "London",
                rating: 4.5m,
                phone: "020",
                website: "https://gamma.example",
                profileUrl));

        var current = CreateRun(
            CreateListing(
                "gamma-office-two",
                "Gamma Law",
                "London",
                rating: 4.5m,
                phone: "020",
                website: "https://gamma.example",
                profileUrl));

        var report = new SearchReportBuilder().Build(current, previous);

        Assert.Equal(0, report.Summary.NewListings);
        Assert.Empty(report.NewListings);
        Assert.Contains(report.ByLocation, location => location.Location == "London" && location.NewListingCount == 0);
    }

    private static SearchRun CreateRun(params SolicitorListing[] listings)
    {
        return new SearchRun(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddMinutes(-1),
            DateTimeOffset.UtcNow,
            listings.Select(listing => listing.Location).Distinct().ToList(),
            listings,
            []);
    }

    private static SolicitorListing CreateListing(
        string key,
        string name,
        string location,
        decimal? rating,
        string? phone,
        string? website,
        string? profileUrl = null)
    {
        return SolicitorListing.Create(
            key,
            name,
            location,
            ContactDetails.Create("Address", phone, website, null, profileUrl ?? $"https://example.com/{key}.html"),
            null,
            Rating.Create(rating, rating.HasValue ? 10 : null),
            [],
            false);
    }
}
