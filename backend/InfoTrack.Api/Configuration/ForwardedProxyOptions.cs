using System.Globalization;
using System.Net;

namespace InfoTrack.Api.Configuration;

public sealed class ForwardedProxyOptions
{
    public const string SectionName = "ForwardedProxy";

    public bool Enabled { get; init; } = true;

    public string[] KnownNetworks { get; init; } = ["172.16.0.0/12"];

    public string[] KnownProxies { get; init; } = [];

    public void Validate()
    {
        foreach (var network in KnownNetworks)
        {
            ParseNetwork(network);
        }

        foreach (var proxy in KnownProxies)
        {
            IPAddress.Parse(proxy);
        }
    }

    public static (IPAddress Prefix, int PrefixLength) ParseNetwork(string value)
    {
        var parts = value.Split('/', 2, StringSplitOptions.TrimEntries);
        if (parts.Length != 2)
        {
            throw new InvalidOperationException(
                $"{SectionName}:KnownNetworks values must use CIDR notation, for example 172.16.0.0/12.");
        }

        var prefix = IPAddress.Parse(parts[0]);
        var prefixLength = int.Parse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture);
        var maxPrefixLength = prefix.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork ? 32 : 128;

        if (prefixLength < 0 || prefixLength > maxPrefixLength)
        {
            throw new InvalidOperationException(
                $"{SectionName}:KnownNetworks contains invalid prefix length '{prefixLength}' for '{value}'.");
        }

        return (prefix, prefixLength);
    }
}
