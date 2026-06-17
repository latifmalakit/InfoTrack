using System.Diagnostics;
using System.Net;
using InfoTrack.Application.Abstractions;
using InfoTrack.Application.LocationSearch;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Solicitors;

public sealed class SolicitorsHttpClient(
    HttpClient httpClient,
    ManualSolicitorsHtmlParser parser,
    ILogger<SolicitorsHttpClient> logger) : ISolicitorSearchClient
{
    private const string ConveyancingAreaId = "192";

    public async Task<SolicitorSearchOutcome> SearchAsync(string location, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["did"] = ConveyancingAreaId,
            ["location"] = location
        });

        logger.LogDebug("Solicitors.com search request started. Location={Location}", location);

        try
        {
            using var response = await httpClient.PostAsync("/prepare-search.asp", content, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return CreateHttpStatusFailure(location, response, stopwatch.ElapsedMilliseconds);
            }

            var html = await response.Content.ReadAsStringAsync(cancellationToken);
            var finalUrl = response.RequestMessage?.RequestUri?.ToString()
                ?? httpClient.BaseAddress?.ToString()
                ?? string.Empty;
            var listings = parser.Parse(html, location);

            logger.LogDebug(
                "Solicitors.com search response parsed. Location={Location} StatusCode={StatusCode} Listings={ListingCount} FinalUrl={FinalUrl} ElapsedMs={ElapsedMilliseconds}",
                location,
                (int)response.StatusCode,
                listings.Count,
                finalUrl,
                stopwatch.ElapsedMilliseconds);

            return SolicitorSearchOutcome.Succeeded(new LocationSearchResult(location, finalUrl, listings));
        }
        catch (HttpRequestException exception)
        {
            logger.LogWarning(
                exception,
                "Solicitors.com search request failed. Location={Location} ElapsedMs={ElapsedMilliseconds}",
                location,
                stopwatch.ElapsedMilliseconds);

            return SolicitorSearchOutcome.UpstreamFailed(
                location,
                "The search provider could not be reached. Try again later.",
                SolicitorSearchUpstreamFailureReason.Network);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "Solicitors.com search request timed out. Location={Location} ElapsedMs={ElapsedMilliseconds}",
                location,
                stopwatch.ElapsedMilliseconds);

            return SolicitorSearchOutcome.UpstreamFailed(
                location,
                "The search provider took too long to respond. Try again later.",
                SolicitorSearchUpstreamFailureReason.Timeout);
        }
    }

    private SolicitorSearchOutcome CreateHttpStatusFailure(
        string location,
        HttpResponseMessage response,
        long elapsedMilliseconds)
    {
        logger.LogWarning(
            "Solicitors.com search returned non-success status. Location={Location} StatusCode={StatusCode} ReasonPhrase={ReasonPhrase} ElapsedMs={ElapsedMilliseconds}",
            location,
            (int)response.StatusCode,
            response.ReasonPhrase,
            elapsedMilliseconds);

        return SolicitorSearchOutcome.UpstreamFailed(
            location,
            CreateUserSafeHttpFailureMessage(response.StatusCode),
            SolicitorSearchUpstreamFailureReason.HttpStatus);
    }

    private static string CreateUserSafeHttpFailureMessage(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        if (code is >= 300 and < 400 || statusCode == HttpStatusCode.NotFound)
        {
            return "No results were returned for this location. Check that it is a supported UK town or city.";
        }

        if (statusCode == HttpStatusCode.TooManyRequests)
        {
            return "The search provider is rate limiting requests. Try again later.";
        }

        if (code >= 500)
        {
            return "The search provider is temporarily unavailable. Try again later.";
        }

        return "The search provider could not return results for this location.";
    }
}
