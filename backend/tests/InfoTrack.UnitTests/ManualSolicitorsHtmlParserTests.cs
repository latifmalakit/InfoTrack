using InfoTrack.Infrastructure.Solicitors;

namespace InfoTrack.UnitTests;

public sealed class ManualSolicitorsHtmlParserTests
{
    [Fact]
    public void Parse_extracts_featured_and_small_result_cards()
    {
        const string html = """
        <div class="result-item">
          <div class="top-holder">
            <span class="h2">Anthony Gold<div class="greentick" title="Anthony Gold hold the following quality marks:
        Lexcel
        The Law Society Conveyancing Quality Scheme. "></div><span class="rev-results"><div class="star-full rating-lrg"></div><div class="star-full rating-lrg"></div><div class="star-full rating-lrg"></div><div class="star-full rating-lrg"></div><div class="star-half rating-lrg"></div> (468)</span></span>
            <div class="phone-block mobile-hidden"><span>Phone:</span><a rel="noindex" href="tel:02079404000">020 7940 4000 </a></div>
          </div>
          <a href="/anthony-gold.html" class="link-map"><i></i><address>London Bridge, London, SE1 2QN</address></a>
          <p>Specialist solicitors in London.</p>
          <ul class="list-item">
            <li class="red-color"><a rel="noindex nofollow" href="/enquiry-form.asp?SiD=NDc3NzA&DiD=MTky">Email</a></li>
            <li><a target="_blank" href="http://www.anthonygold.co.uk" rel="nofollow">Website</a></li>
          </ul>
        </div>
        <div class="result-item item-small">
          <span class="h2">Gardian Solicitors<div class="greentick-small" title="Gardian Solicitors hold the following quality marks:
        The Law Society Conveyancing Quality Scheme. "></div><span class="rev-results"><div class="star-full rating-sml pad-top"></div><div class="star-full rating-sml pad-top"></div><div class="star-full rating-sml pad-top"></div><div class="star-full rating-sml pad-top"></div><div class="star-half rating-sml pad-top"></div>&nbsp;(90)</span></span>
          <a href="/gardian-solicitors.html" class="link-map"><i></i><address>Solar House, London&nbsp;N12 8QJ</address></a>
          <a class="tel" rel="noindex" href="tel:02083436024">0208 343 6024</a>
          <p>Buying or selling domestic property.</p>
        </div>
        """;

        var parser = new ManualSolicitorsHtmlParser(new SolicitorsClientOptions());

        var listings = parser.Parse(html, "London");

        Assert.Equal(2, listings.Count);

        var featured = listings[0];
        Assert.Equal("Anthony Gold", featured.Name);
        Assert.Equal("London", featured.Location);
        Assert.Equal("020 7940 4000", featured.ContactDetails.PhoneNumber);
        Assert.Equal("https://www.solicitors.com/anthony-gold.html", featured.ContactDetails.ProfileUrl);
        Assert.Equal("http://www.anthonygold.co.uk/", featured.ContactDetails.WebsiteUrl);
        Assert.Equal("https://www.solicitors.com/enquiry-form.asp?SiD=NDc3NzA&DiD=MTky", featured.ContactDetails.ContactFormUrl);
        Assert.Equal(4.5m, featured.Rating.Score);
        Assert.Equal(468, featured.Rating.ReviewCount);
        Assert.Contains("Lexcel", featured.QualityMarks);
        Assert.True(featured.IsFeatured);

        var small = listings[1];
        Assert.Equal("Gardian Solicitors", small.Name);
        Assert.Equal("Solar House, London N12 8QJ", small.ContactDetails.Address);
        Assert.Equal(4.5m, small.Rating.Score);
        Assert.Equal(90, small.Rating.ReviewCount);
        Assert.False(small.IsFeatured);
    }

    [Fact]
    public void Parse_does_not_treat_phone_area_code_as_review_count()
    {
        const string html = """
        <div class="result-item">
          <span class="h2">Phone First Solicitors</span>
          <a class="tel" rel="noindex" href="tel:02079404000">(020) 7940 4000</a>
          <a href="/phone-first-solicitors.html" class="link-map"><i></i><address>London, SE1 2QN</address></a>
          <p>Residential conveyancing specialists.</p>
        </div>
        """;

        var parser = new ManualSolicitorsHtmlParser(new SolicitorsClientOptions());

        var listing = Assert.Single(parser.Parse(html, "London"));

        Assert.Equal("(020) 7940 4000", listing.ContactDetails.PhoneNumber);
        Assert.Null(listing.Rating.ReviewCount);
    }

    [Fact]
    public void Parse_ignores_non_http_urls_from_source_html()
    {
        const string html = """
        <div class="result-item">
          <span class="h2">Safe Link Solicitors</span>
          <a href="/safe-link-solicitors.html" class="link-map"><i></i><address>London, SE1 2QN</address></a>
          <ul class="list-item">
            <li><a target="_blank" href="javascript:alert(1)" rel="nofollow">Website</a></li>
          </ul>
        </div>
        """;

        var parser = new ManualSolicitorsHtmlParser(new SolicitorsClientOptions());

        var listing = Assert.Single(parser.Parse(html, "London"));

        Assert.Null(listing.ContactDetails.WebsiteUrl);
        Assert.Equal("https://www.solicitors.com/safe-link-solicitors.html", listing.ContactDetails.ProfileUrl);
    }
}
