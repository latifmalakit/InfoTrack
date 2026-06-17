using System.Diagnostics;
using InfoTrack.Application.Abstractions;
using InfoTrack.Application.Locations;
using InfoTrack.Application.LocationSearch;
using InfoTrack.Application.SearchRuns.Reports;
using InfoTrack.Domain.Locations;
using InfoTrack.Domain.SearchRuns;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Application.SearchRuns;

public sealed class RunSolicitorSearchHandler(
    LocationSearchOrchestrator searchOrchestrator,
    ISearchRunRepository repository,
    SearchReportBuilder reportBuilder,
    ILogger<RunSolicitorSearchHandler> logger)
{
    private const int MaxLocations = 20;

    public async Task<SearchRunReport> HandleAsync(
        RunSolicitorSearchRequest request,
        CancellationToken cancellationToken)
    {
        var locations = NormalizeLocations(request.Locations);
        if (locations.Count == 0)
        {
            throw new SearchValidationException("At least one location is required.");
        }

        if (locations.Count > MaxLocations)
        {
            throw new SearchValidationException($"No more than {MaxLocations} locations can be searched at once.");
        }

        var stopwatch = Stopwatch.StartNew();
        var startedAt = DateTimeOffset.UtcNow;
        logger.LogInformation(
            "Search run started. Locations={LocationCount} CompareWithPreviousRun={CompareWithPreviousRun}",
            locations.Count,
            request.CompareWithPreviousRun);

        var previousRun = request.CompareWithPreviousRun
            ? await repository.GetLatestCompletedBeforeAsync(startedAt, cancellationToken)
            : null;

        logger.LogDebug(
            "Previous completed run lookup finished. PreviousRunFound={PreviousRunFound}",
            previousRun is not null);

        var searchBatch = await searchOrchestrator.SearchAsync(locations, cancellationToken);
        var completedAt = DateTimeOffset.UtcNow;
        var run = new SearchRun(
            Guid.NewGuid(),
            startedAt,
            completedAt,
            locations,
            searchBatch.Listings,
            searchBatch.Failures);

        await repository.SaveAsync(run, cancellationToken);

        var report = reportBuilder.Build(run, previousRun);
        logger.LogInformation(
            "Search run completed. RunId={RunId} Listings={ListingCount} FailedLocations={FailedLocationCount} NewListings={NewListingCount} ElapsedMs={ElapsedMilliseconds}",
            run.Id,
            report.Summary.TotalListings,
            report.Summary.FailedLocations,
            report.Summary.NewListings,
            stopwatch.ElapsedMilliseconds);

        return report;
    }

    private static List<string> NormalizeLocations(IEnumerable<string>? locations)
    {
        try
        {
            return (locations ?? DefaultLocations.Values)
                .Select(location => location?.Trim())
                .Where(location => !string.IsNullOrWhiteSpace(location))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(location => new Location(location!).Name)
                .ToList();
        }
        catch (ArgumentException exception)
        {
            throw new SearchValidationException(exception.Message);
        }
    }
}
