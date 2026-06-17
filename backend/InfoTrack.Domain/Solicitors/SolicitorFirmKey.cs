using System.Security.Cryptography;
using System.Text;

namespace InfoTrack.Domain.Solicitors;

public static class SolicitorFirmKey
{
    public static string Create(SolicitorListing listing)
    {
        return Create(
            listing.ContactDetails.ProfileUrl,
            listing.Name,
            listing.Location);
    }

    public static string Create(string? profileUrl, string name, string location)
    {
        var identityPart = string.IsNullOrWhiteSpace(profileUrl) ? Normalize(name) : Normalize(profileUrl);
        var raw = string.Join('|', identityPart, Normalize(location));
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string Normalize(string value)
    {
        return string.Join(' ', value.Trim().ToLowerInvariant().Split(
            ' ',
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
    }
}
