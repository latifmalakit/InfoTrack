using System.Text.Json.Serialization;

namespace InfoTrack.Domain.Solicitors;

public sealed record ContactDetails
{
    public static ContactDetails Empty { get; } = new(null, null, null, null, null);

    [JsonConstructor]
    private ContactDetails(
        string? address,
        string? phoneNumber,
        string? websiteUrl,
        string? contactFormUrl,
        string? profileUrl)
    {
        Address = NormalizeText(address);
        PhoneNumber = NormalizeText(phoneNumber);
        WebsiteUrl = NormalizeUrl(websiteUrl, nameof(websiteUrl));
        ContactFormUrl = NormalizeUrl(contactFormUrl, nameof(contactFormUrl));
        ProfileUrl = NormalizeUrl(profileUrl, nameof(profileUrl));
    }

    public string? Address { get; }

    public string? PhoneNumber { get; }

    public string? WebsiteUrl { get; }

    public string? ContactFormUrl { get; }

    public string? ProfileUrl { get; }

    public static ContactDetails Create(
        string? address,
        string? phoneNumber,
        string? websiteUrl,
        string? contactFormUrl,
        string? profileUrl)
    {
        return new ContactDetails(address, phoneNumber, websiteUrl, contactFormUrl, profileUrl);
    }

    private static string? NormalizeText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static string? NormalizeUrl(string? value, string parameterName)
    {
        var normalized = NormalizeText(value);
        if (normalized is null)
        {
            return null;
        }

        if (!Uri.TryCreate(normalized, UriKind.Absolute, out var uri) ||
            (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new ArgumentException("URL must be an absolute HTTP or HTTPS URL.", parameterName);
        }

        return uri.ToString();
    }
}
