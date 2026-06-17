using System.Collections.Concurrent;
using InfoTrack.Application.Abstractions;
using InfoTrack.Domain.SearchRuns;
using Microsoft.Extensions.Logging;

namespace InfoTrack.Infrastructure.Persistence;

public sealed class InMemorySearchRunRepository : ISearchRunRepository
{
    private readonly ConcurrentDictionary<Guid, SearchRun> _runs = new();
    private readonly ILogger<InMemorySearchRunRepository> _logger;
    private readonly int _maxStoredRuns;

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
        _runs[run.Id] = run;
        _logger.LogDebug(
            "Search run saved in memory. RunId={RunId} StoredRuns={StoredRunCount}",
            run.Id,
            _runs.Count);

        PruneOldRuns();
        return Task.CompletedTask;
    }

    public Task<SearchRun?> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        _runs.TryGetValue(id, out var run);
        return Task.FromResult(run);
    }

    public Task<SearchRun?> GetLatestCompletedBeforeAsync(DateTimeOffset timestamp, CancellationToken cancellationToken)
    {
        var run = _runs.Values
            .Where(candidate => candidate.CompletedAtUtc < timestamp)
            .OrderByDescending(candidate => candidate.CompletedAtUtc)
            .FirstOrDefault();

        return Task.FromResult(run);
    }

    public Task<IReadOnlyList<SearchRun>> GetRecentAsync(int count, CancellationToken cancellationToken)
    {
        IReadOnlyList<SearchRun> runs = _runs.Values
            .OrderByDescending(run => run.CompletedAtUtc)
            .Take(Math.Max(0, count))
            .ToList();

        return Task.FromResult(runs);
    }

    private void PruneOldRuns()
    {
        var expiredRunIds = _runs.Values
            .OrderByDescending(run => run.CompletedAtUtc)
            .Skip(_maxStoredRuns)
            .Select(run => run.Id)
            .ToList();

        foreach (var runId in expiredRunIds)
        {
            _runs.TryRemove(runId, out _);
        }

        if (expiredRunIds.Count > 0)
        {
            _logger.LogDebug(
                "Old in-memory search runs pruned. RemovedRuns={RemovedRunCount} StoredRuns={StoredRunCount}",
                expiredRunIds.Count,
                _runs.Count);
        }
    }
}
