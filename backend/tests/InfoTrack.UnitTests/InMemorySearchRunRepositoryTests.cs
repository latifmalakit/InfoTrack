using InfoTrack.Domain.SearchRuns;
using InfoTrack.Domain.Solicitors;
using InfoTrack.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace InfoTrack.UnitTests;

public sealed class InMemorySearchRunRepositoryTests
{
    [Fact]
    public async Task SaveAsync_retains_only_recent_runs()
    {
        var repository = CreateRepository();

        for (var index = 0; index < 30; index++)
        {
            await repository.SaveAsync(CreateRun(index), CancellationToken.None);
        }

        var recent = await repository.GetRecentAsync(100, CancellationToken.None);

        Assert.Equal(25, recent.Count);
        Assert.DoesNotContain(recent, run => run.Listings.Count == 0);
        Assert.Contains(recent, run => run.Listings.Count == 29);
    }

    [Fact]
    public async Task SaveAsync_uses_configured_max_stored_runs()
    {
        var repository = CreateRepository(maxStoredRuns: 3);

        for (var index = 0; index < 5; index++)
        {
            await repository.SaveAsync(CreateRun(index), CancellationToken.None);
        }

        var recent = await repository.GetRecentAsync(100, CancellationToken.None);

        Assert.Equal(3, recent.Count);
        Assert.DoesNotContain(recent, run => run.Listings.Count == 0);
        Assert.DoesNotContain(recent, run => run.Listings.Count == 1);
        Assert.Contains(recent, run => run.Listings.Count == 4);
    }

    [Fact]
    public async Task GetRecentAsync_returns_empty_list_when_count_is_zero()
    {
        var repository = CreateRepository();
        await repository.SaveAsync(CreateRun(1), CancellationToken.None);

        var recent = await repository.GetRecentAsync(0, CancellationToken.None);

        Assert.Empty(recent);
    }

    [Fact]
    public async Task SaveAsync_honors_cancellation()
    {
        var repository = CreateRepository();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAsync<OperationCanceledException>(() =>
            repository.SaveAsync(CreateRun(1), cancellation.Token));
    }

    private static InMemorySearchRunRepository CreateRepository(int maxStoredRuns = 25)
    {
        return new InMemorySearchRunRepository(
            new SearchRunStorageOptions { MaxStoredRuns = maxStoredRuns },
            NullLogger<InMemorySearchRunRepository>.Instance);
    }

    private static SearchRun CreateRun(int index)
    {
        var timestamp = DateTimeOffset.UtcNow.AddMinutes(index);
        var listings = Enumerable.Range(0, index)
            .Select(listingIndex => SolicitorListing.Create(
                $"key-{index}-{listingIndex}",
                $"Firm {index}-{listingIndex}",
                "London",
                ContactDetails.Create("Address", "020", null, null, null),
                null,
                Rating.Empty,
                [],
                false))
            .ToList();

        return new SearchRun(
            Guid.NewGuid(),
            timestamp.AddSeconds(-1),
            timestamp,
            ["London"],
            listings,
            []);
    }
}
