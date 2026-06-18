using System.Net;
using System.Text.RegularExpressions;
using InfoTrack.Domain.Solicitors;

namespace InfoTrack.Infrastructure.Solicitors;

public sealed partial class ManualSolicitorsHtmlParser(SolicitorsClientOptions options)
{
    private readonly Uri _baseUri = options.GetBaseAddress();

    public IReadOnlyList<SolicitorListing> Parse(string html, string location)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return [];
        }

        return ExtractResultBlocks(html)
            .Select(block => ParseListing(block, location))
            .Where(listing => listing is not null)
            .Cast<SolicitorListing>()
            .ToList();
    }

    private static IEnumerable<string> ExtractResultBlocks(string html)
    {
        var matches = ResultItemStartRegex().Matches(html);
        for (var index = 0; index < matches.Count; index++)
        {
            var start = matches[index].Index;
            var end = index + 1 < matches.Count ? matches[index + 1].Index : html.Length;
            yield return html[start..end];
        }
    }

    private SolicitorListing? ParseListing(string block, string location)
    {
        var name = ExtractName(block);
        if (string.IsNullOrWhiteSpace(name))
        {
            return null;
        }

        var address = ExtractFirstGroup(block, AddressRegex());
        var phone = ExtractPhone(block);
        var website = ToAbsoluteUrl(ExtractWebsite(block));
        var contactForm = ToAbsoluteUrl(ExtractFirstGroup(block, ContactFormRegex()));
        var profileUrl = ToAbsoluteUrl(ExtractFirstGroup(block, ProfileRegex()));
        var description = ExtractDescription(block);
        var qualityMarks = ExtractQualityMarks(block);
        var rating = ExtractRating(block);
        var externalKey = SolicitorListingKey.Create(profileUrl, name, phone, address, location);
        var isFeatured = !block.Contains("item-small", StringComparison.OrdinalIgnoreCase);

        return SolicitorListing.Create(
            externalKey,
            name,
            location,
            ContactDetails.Create(address, phone, website, contactForm, profileUrl),
            description,
            rating,
            qualityMarks,
            isFeatured);
    }

    private static string? ExtractName(string block)
    {
        var match = NameRegex().Match(block);
        if (!match.Success)
        {
            return null;
        }

        return CleanText(match.Groups["value"].Value);
    }

    private static string? ExtractPhone(string block)
    {
        var match = TelRegex().Match(block);
        if (match.Success)
        {
            return CleanText(match.Groups["text"].Value);
        }

        return null;
    }

    private static string? ExtractWebsite(string block)
    {
        foreach (Match match in WebsiteRegex().Matches(block))
        {
            var text = CleanText(match.Groups["text"].Value);
            if (text.Contains("website", StringComparison.OrdinalIgnoreCase))
            {
                return match.Groups["url"].Value;
            }
        }

        return null;
    }

    private static string? ExtractDescription(string block)
    {
        var match = ParagraphRegex().Match(block);
        return match.Success ? CleanText(match.Groups["value"].Value) : null;
    }

    private static IReadOnlyList<string> ExtractQualityMarks(string block)
    {
        var match = QualityMarksRegex().Match(block);
        if (!match.Success)
        {
            return [];
        }

        var title = WebUtility.HtmlDecode(match.Groups["title"].Value);
        var parts = title.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts
            .SkipWhile(part => !part.Contains("quality marks", StringComparison.OrdinalIgnoreCase))
            .Skip(1)
            .Select(part => part.Trim(' ', '.'))
            .Where(part => !string.IsNullOrWhiteSpace(part))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static Rating ExtractRating(string block)
    {
        var fullStars = FullStarRegex().Matches(block).Count;
        var halfStars = HalfStarRegex().Matches(block).Count;
        decimal? score = fullStars == 0 && halfStars == 0 ? null : fullStars + halfStars * 0.5m;

        var reviewMatch = ReviewCountRegex().Match(block);
        var reviewCount = reviewMatch.Success && int.TryParse(reviewMatch.Groups["count"].Value, out var count)
            ? count
            : (int?)null;

        return Rating.Create(score, reviewCount);
    }

    private static string? ExtractFirstGroup(string block, Regex regex)
    {
        var match = regex.Match(block);
        return match.Success ? CleanText(match.Groups["value"].Value) : null;
    }

    private string? ToAbsoluteUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }

        return Uri.TryCreate(url, UriKind.Absolute, out var absolute) &&
               (absolute.Scheme == Uri.UriSchemeHttp || absolute.Scheme == Uri.UriSchemeHttps)
            ? absolute.ToString()
            : new Uri(_baseUri, url).ToString();
    }

    private static string CleanText(string value)
    {
        var withoutTags = TagsRegex().Replace(value, " ");
        var decoded = WebUtility.HtmlDecode(withoutTags).Replace('\u00a0', ' ');
        return string.Join(' ', decoded.Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }

    [GeneratedRegex("<div\\s+class=\"[^\"]*\\bresult-item\\b[^\"]*\"", RegexOptions.IgnoreCase)]
    private static partial Regex ResultItemStartRegex();

    [GeneratedRegex("<span\\s+class=\"h2\">(?<value>.*?)(?:<div\\s+class=\"greentick|<span\\s+class=\"rev-results|</span>)", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex NameRegex();

    [GeneratedRegex("<address>(?<value>.*?)</address>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex AddressRegex();

    [GeneratedRegex("<a[^>]+href=\"tel:[^\"]*\"[^>]*>(?<text>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TelRegex();

    [GeneratedRegex("<a[^>]+href=\"(?<url>[^\"]+)\"[^>]*>(?<text>.*?)</a>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex WebsiteRegex();

    [GeneratedRegex("<a[^>]+href=\"(?<value>/enquiry-form\\.asp[^\"]*)\"", RegexOptions.IgnoreCase)]
    private static partial Regex ContactFormRegex();

    [GeneratedRegex("<a\\s+href=\"(?<value>/[^\"]+\\.html)\"\\s+class=\"link-map\"", RegexOptions.IgnoreCase)]
    private static partial Regex ProfileRegex();

    [GeneratedRegex("<p>(?<value>.*?)</p>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ParagraphRegex();

    [GeneratedRegex("<div\\s+class=\"greentick(?:-small)?\"\\s+title=\"(?<title>[^\"]*)\"", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex QualityMarksRegex();

    [GeneratedRegex("star-full\\s+rating", RegexOptions.IgnoreCase)]
    private static partial Regex FullStarRegex();

    [GeneratedRegex("star-half\\s+rating", RegexOptions.IgnoreCase)]
    private static partial Regex HalfStarRegex();

    [GeneratedRegex("<span\\s+class=\"[^\"]*\\brev-results\\b[^\"]*\"[^>]*>.*?\\((?<count>\\d+)\\).*?</span>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ReviewCountRegex();

    [GeneratedRegex("<.*?>", RegexOptions.Singleline)]
    private static partial Regex TagsRegex();
}
