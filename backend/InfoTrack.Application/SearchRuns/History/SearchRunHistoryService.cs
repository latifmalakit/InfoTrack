using InfoTrack.Application.Abstractions;
using InfoTrack.Application.SearchRuns.Reports;
using InfoTrack.Domain.SearchRuns;

namespace InfoTrack.Application.SearchRuns.History;

public sealed class SearchRunHistoryService(
    ISearchRunRepository repository,
    SearchReportBuilder reportBuilder)
{
    private const int DefaultRecentRunCount = 10;

    public async Task<IReadOnlyList<SearchRunListItem>> GetRecentAsync(CancellationToken cancellationToken)
    {
        var runs = await repository.GetRecentAsync(DefaultRecentRunCount, cancellationToken);

        return runs
            .Select(ToListItem)
            .ToList();
    }

    public async Task<SearchRunReport?> GetReportAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await repository.GetAsync(runId, cancellationToken);
        if (run is null)
        {
            return null;
        }

        var previousRun = await repository.GetLatestCompletedBeforeAsync(run.StartedAtUtc, cancellationToken);
        return reportBuilder.Build(run, previousRun);
    }

    private static SearchRunListItem ToListItem(SearchRun run)
    {
        return new SearchRunListItem(
            run.Id,
            run.StartedAtUtc,
            run.CompletedAtUtc,
            run.Listings.Count,
            run.Locations.Count,
            run.Failures.Count);
    }
}
