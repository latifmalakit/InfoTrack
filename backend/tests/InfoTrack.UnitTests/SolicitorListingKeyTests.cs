using InfoTrack.Domain.Solicitors;

namespace InfoTrack.UnitTests;

public sealed class SolicitorListingKeyTests
{
    [Fact]
    public void Create_is_stable_for_same_listing_identity()
    {
        var first = SolicitorListingKey.Create(" HTTPS://Example.com/Profile.HTML ", "Name", "1", "Address", "London");
        var second = SolicitorListingKey.Create("https://example.com/profile.html", "Other display", "1", "Address", "london");

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
    }

    [Fact]
    public void Create_distinguishes_multiple_offices_on_same_profile()
    {
        var first = SolicitorListingKey.Create("https://example.com/profile.html", "Name", "1", "Address A", "London");
        var second = SolicitorListingKey.Create("https://example.com/profile.html", "Name", "1", "Address B", "London");

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void Create_hashes_normalized_listing_identity_when_profile_url_is_missing()
    {
        var first = SolicitorListingKey.Create(null, "Alpha\t\u00a0Law", " 020\n1000 ", "1 Main Street", "Leeds");
        var second = SolicitorListingKey.Create("", "alpha law", "020 1000", "1 Main Street", "leeds");

        Assert.Equal(first, second);
        Assert.Equal(64, first.Length);
    }

    [Fact]
    public void Firm_key_normalizes_all_whitespace()
    {
        var first = SolicitorFirmKey.Create(null, "Alpha\t\u00a0Law", "London\nBridge");
        var second = SolicitorFirmKey.Create(null, "alpha law", "london bridge");

        Assert.Equal(first, second);
    }
}
