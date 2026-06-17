namespace InfoTrack.Domain.Locations;

public sealed record Location
{
    private const int MaxNameLength = 80;

    public Location(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Location is required.", nameof(name));
        }

        var normalizedName = name.Trim();
        if (normalizedName.Length > MaxNameLength)
        {
            throw new ArgumentException($"Location cannot exceed {MaxNameLength} characters.", nameof(name));
        }

        if (normalizedName.Any(character => !IsSupportedLocationCharacter(character)))
        {
            throw new ArgumentException("Location contains unsupported characters.", nameof(name));
        }

        Name = normalizedName;
    }

    public string Name { get; }

    private static bool IsSupportedLocationCharacter(char character)
    {
        return char.IsLetterOrDigit(character) ||
               character is ' ' or '-' or '\'' or '.';
    }
}
