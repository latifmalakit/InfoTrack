using InfoTrack.Domain.SearchRuns;

namespace InfoTrack.Application.Abstractions;

public interface ISearchRunRepository
{
    Task SaveAsync(SearchRun run, CancellationToken cancellationToken);

    Task<SearchRun?> GetAsync(Guid id, CancellationToken cancellationToken);

    Task<SearchRun?> GetLatestCompletedBeforeAsync(DateTimeOffset timestamp, CancellationToken cancellationToken);

    Task<IReadOnlyList<SearchRun>> GetRecentAsync(int count, CancellationToken cancellationToken);
}
