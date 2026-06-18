using InfoTrack.Domain.Solicitors;
using InfoTrack.Domain.SearchRuns;

namespace InfoTrack.UnitTests;

public sealed class SolicitorDomainConstructionTests
{
    [Theory]
    [InlineData(-0.5)]
    [InlineData(5.5)]
    public void Rating_rejects_scores_outside_five_star_range(decimal score)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Rating.Create(score, 1));
    }

    [Fact]
    public void Rating_rejects_negative_review_count()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Rating.Create(4.5m, -1));
    }

    [Fact]
    public void ContactDetails_normalizes_empty_values_and_rejects_non_http_urls()
    {
        var contactDetails = ContactDetails.Create(
            "  1 Test Street  ",
            " 020 0000 0000 ",
            null,
            " ",
            "https://example.com/profile");

        Assert.Equal("1 Test Street", contactDetails.Address);
        Assert.Equal("020 0000 0000", contactDetails.PhoneNumber);
        Assert.Null(contactDetails.WebsiteUrl);
        Assert.Null(contactDetails.ContactFormUrl);
        Assert.Equal("https://example.com/profile", contactDetails.ProfileUrl);
        Assert.Throws<ArgumentException>(() => ContactDetails.Create(null, null, "ftp://example.com", null, null));
    }

    [Fact]
    public void SolicitorListing_requires_identity_fields_and_normalizes_quality_marks()
    {
        var listing = SolicitorListing.Create(
            " key-1 ",
            " Alpha Law ",
            " London ",
            ContactDetails.Empty,
            " ",
            Rating.Empty,
            [" Lexcel ", "", "lexcel", "CQS"],
            false);

        Assert.Equal("key-1", listing.ExternalKey);
        Assert.Equal("Alpha Law", listing.Name);
        Assert.Equal("London", listing.Location);
        Assert.Null(listing.Description);
        Assert.Equal(["Lexcel", "CQS"], listing.QualityMarks);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)listing.QualityMarks).Add("Mutated"));
        Assert.Throws<ArgumentException>(() => SolicitorListing.Create(
            "",
            "Alpha Law",
            "London",
            ContactDetails.Empty,
            null,
            Rating.Empty,
            [],
            false));
    }

    [Fact]
    public void SearchRun_snapshots_input_lists()
    {
        var locations = new List<string> { "London" };
        var listings = new List<SolicitorListing>
        {
            SolicitorListing.Create(
                "key-1",
                "Alpha Law",
                "London",
                ContactDetails.Empty,
                null,
                Rating.Empty,
                [],
                false)
        };

        var run = new SearchRun(
            Guid.NewGuid(),
            DateTimeOffset.UtcNow.AddSeconds(-1),
            DateTimeOffset.UtcNow,
            locations,
            listings,
            []);

        locations.Add("Leeds");
        listings.Clear();

        Assert.Equal(["London"], run.Locations);
        Assert.Single(run.Listings);
        Assert.Throws<NotSupportedException>(() => ((IList<string>)run.Locations).Add("Mutated"));
    }
}
