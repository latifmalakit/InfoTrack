using System.Text.Json.Serialization;

namespace InfoTrack.Domain.Solicitors;

public sealed record SolicitorListing
{
    [JsonConstructor]
    private SolicitorListing(
        string externalKey,
        string name,
        string location,
        ContactDetails contactDetails,
        string? description,
        Rating rating,
        IReadOnlyList<string>? qualityMarks,
        bool isFeatured)
    {
        ExternalKey = RequireText(externalKey, nameof(externalKey));
        Name = RequireText(name, nameof(name));
        Location = RequireText(location, nameof(location));
        ContactDetails = contactDetails ?? throw new ArgumentNullException(nameof(contactDetails));
        Description = NormalizeOptionalText(description);
        Rating = rating ?? throw new ArgumentNullException(nameof(rating));
        QualityMarks = NormalizeQualityMarks(qualityMarks);
        IsFeatured = isFeatured;
    }

    public string ExternalKey { get; }

    public string Name { get; }

    public string Location { get; }

    public ContactDetails ContactDetails { get; }

    public string? Description { get; }

    public Rating Rating { get; }

    public IReadOnlyList<string> QualityMarks { get; }

    public bool IsFeatured { get; }

    public static SolicitorListing Create(
        string externalKey,
        string name,
        string location,
        ContactDetails contactDetails,
        string? description,
        Rating rating,
        IReadOnlyList<string>? qualityMarks,
        bool isFeatured)
    {
        return new SolicitorListing(
            externalKey,
            name,
            location,
            contactDetails,
            description,
            rating,
            qualityMarks,
            isFeatured);
    }

    private static string RequireText(string? value, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Value is required.", parameterName);
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    private static IReadOnlyList<string> NormalizeQualityMarks(IReadOnlyList<string>? qualityMarks)
    {
        var normalizedMarks = (qualityMarks ?? [])
            .Select(NormalizeOptionalText)
            .Where(mark => mark is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return Array.AsReadOnly(normalizedMarks);
    }
}
