using System.Security.Cryptography;
using System.Text;

namespace InfoTrack.Domain.Solicitors;

public static class SolicitorListingKey
{
    public static string Create(
        string? profileUrl,
        string name,
        string? phoneNumber,
        string? address,
        string? location = null)
    {
        var profilePart = string.IsNullOrWhiteSpace(profileUrl) ? Normalize(name) : Normalize(profileUrl);
        var raw = string.Join(
            '|',
            profilePart,
            Normalize(location ?? string.Empty),
            Normalize(address ?? string.Empty),
            Normalize(phoneNumber ?? string.Empty));
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
