using System.Collections.Concurrent;
using System.Diagnostics;
using InfoTrack.Application.Abstractions;
using InfoTrack.Domain.Locations;
using InfoTrack.Domain.Solicitors;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Application.LocationSearch;

public sealed class LocationSearchOrchestrator(
    ISolicitorSearchClient searchClient,
    ILogger<LocationSearchOrchestrator> logger)
{
    private const int MaxConcurrency = 2;

    public async Task<LocationSearchBatch> SearchAsync(
        IReadOnlyList<string> locations,
        CancellationToken cancellationToken)
    {
        var listings = new ConcurrentBag<SolicitorListing>();
        var failures = new ConcurrentBag<LocationSearchFailure>();
        using var semaphore = new SemaphoreSlim(MaxConcurrency);

        logger.LogInformation(
            "Location search batch started. Locations={LocationCount} MaxConcurrency={MaxConcurrency}",
            locations.Count,
            MaxConcurrency);

        var tasks = locations.Select(location => SearchLocationAsync(
            location,
            semaphore,
            listings,
            failures,
            cancellationToken));

        await Task.WhenAll(tasks);

        var batch = new LocationSearchBatch(
            listings.OrderBy(listing => listing.Location).ThenBy(listing => listing.Name).ToList(),
            failures.OrderBy(failure => failure.Location).ToList());

        logger.LogInformation(
            "Location search batch completed. Listings={ListingCount} FailedLocations={FailedLocationCount}",
            batch.Listings.Count,
            batch.Failures.Count);

        return batch;
    }

    private async Task SearchLocationAsync(
        string location,
        SemaphoreSlim semaphore,
        ConcurrentBag<SolicitorListing> listings,
        ConcurrentBag<LocationSearchFailure> failures,
        CancellationToken cancellationToken)
    {
        await semaphore.WaitAsync(cancellationToken);
        var stopwatch = Stopwatch.StartNew();

        try
        {
            logger.LogDebug("Location search started. Location={Location}", location);
            var outcome = await searchClient.SearchAsync(location, cancellationToken);

            switch (outcome)
            {
                case SolicitorSearchSucceeded success:
                    AddSuccessfulResult(success.Result, listings, stopwatch.ElapsedMilliseconds);
                    break;

                case SolicitorSearchUpstreamFailure failure:
                    AddUpstreamFailure(failure, failures, stopwatch.ElapsedMilliseconds);
                    break;

                default:
                    throw new InvalidOperationException($"Unknown solicitor search outcome type: {outcome.GetType().Name}.");
            }
        }
        finally
        {
            semaphore.Release();
        }
    }

    private void AddSuccessfulResult(
        LocationSearchResult result,
        ConcurrentBag<SolicitorListing> listings,
        long elapsedMilliseconds)
    {
        foreach (var listing in result.Listings)
        {
            listings.Add(listing);
        }

        logger.LogInformation(
            "Location search completed. Location={Location} Listings={ListingCount} ElapsedMs={ElapsedMilliseconds}",
            result.Location,
            result.Listings.Count,
            elapsedMilliseconds);
    }

    private void AddUpstreamFailure(
        SolicitorSearchUpstreamFailure failure,
        ConcurrentBag<LocationSearchFailure> failures,
        long elapsedMilliseconds)
    {
        failures.Add(new LocationSearchFailure(failure.Location, failure.Error));
        logger.LogWarning(
            "Location search failed because the upstream search provider was unavailable. Location={Location} Reason={FailureReason} Error={Error} ElapsedMs={ElapsedMilliseconds}",
            failure.Location,
            failure.Reason,
            failure.Error,
            elapsedMilliseconds);
    }
}
