using InfoTrack.Application.Abstractions;
using InfoTrack.Domain.SearchRuns;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Persistence;

public sealed class InMemorySearchRunRepository : ISearchRunRepository
{
    private readonly Dictionary<Guid, SearchRun> _runs = [];
    private readonly ILogger<InMemorySearchRunRepository> _logger;
    private readonly int _maxStoredRuns;
    private readonly object _syncRoot = new();

    public InMemorySearchRunRepository(
        SearchRunStorageOptions options,
        ILogger<InMemorySearchRunRepository> logger)
    {
        options.Validate();

        _logger = logger;
        _maxStoredRuns = options.MaxStoredRuns;
    }

    public Task SaveAsync(SearchRun run, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        int storedRunCount;
        int removedRunCount;

        lock (_syncRoot)
        {
            _runs[run.Id] = run;
            removedRunCount = PruneOldRuns();
            storedRunCount = _runs.Count;
        }

        _logger.LogDebug(
            "Search run saved in memory. RunId={RunId} StoredRuns={StoredRunCount}",
            run.Id,
            storedRunCount);

        if (removedRunCount > 0)
        {
            _logger.LogDebug(
                "Old in-memory search runs pruned. RemovedRuns={RemovedRunCount} StoredRuns={StoredRunCount}",
                removedRunCount,
                storedRunCount);
        }

        return Task.CompletedTask;
    }

    public Task<SearchRun?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SearchRun? run;
        lock (_syncRoot)
        {
            _runs.TryGetValue(id, out run);
        }

        return Task.FromResult(run);
    }

    public Task<SearchRun?> GetLatestCompletedBeforeAsync(DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        SearchRun? run;
        lock (_syncRoot)
        {
            run = _runs.Values
                .Where(candidate => candidate.CompletedAtUtc < timestamp)
                .OrderByDescending(candidate => candidate.CompletedAtUtc)
                .FirstOrDefault();
        }

        return Task.FromResult(run);
    }

    public Task<IReadOnlyList<SearchRun>> GetRecentAsync(int count, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        IReadOnlyList<SearchRun> runs;
        lock (_syncRoot)
        {
            runs = _runs.Values
                .OrderByDescending(run => run.CompletedAtUtc)
                .Take(Math.Max(0, count))
                .ToList();
        }

        return Task.FromResult(runs);
    }

    private int PruneOldRuns()
    {
        var expiredRunIds = _runs.Values
            .OrderByDescending(run => run.CompletedAtUtc)
            .Skip(_maxStoredRuns)
            .Select(run => run.Id)
            .ToList();

        foreach (var runId in expiredRunIds)
        {
            _runs.Remove(runId);
        }

        return expiredRunIds.Count;
    }
}
