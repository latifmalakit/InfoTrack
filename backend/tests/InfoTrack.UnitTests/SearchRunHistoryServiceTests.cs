using InfoTrack.Application.SearchRuns.History;
using InfoTrack.Application.SearchRuns.Reports;
using InfoTrack.Domain.SearchRuns;
using InfoTrack.Domain.Solicitors;
using InfoTrack.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace InfoTrack.UnitTests;

public sealed class SearchRunHistoryServiceTests
{
    [Fact]
    public async Task GetReportAsync_builds_report_with_previous_run_comparison()
    {
        var repository = new InMemorySearchRunRepository(
            new SearchRunStorageOptions(),
            NullLogger<InMemorySearchRunRepository>.Instance);
        var history = new SearchRunHistoryService(repository, new SearchReportBuilder());

        await repository.SaveAsync(
            CreateRun(DateTimeOffset.UtcNow.AddMinutes(-2), CreateListing("old-key", "Alpha Law")),
            CancellationToken.None);
        var current = CreateRun(
            DateTimeOffset.UtcNow,
            CreateListing("old-key", "Alpha Law"),
            CreateListing("new-key", "Beta Law"));
        await repository.SaveAsync(current, CancellationToken.None);

        var report = await history.GetReportAsync(current.Id, CancellationToken.None);

        Assert.NotNull(report);
        Assert.Equal(2, report.Summary.TotalListings);
        Assert.Equal(1, report.Summary.NewListings);
        Assert.Contains(report.NewListings, listing => listing.ExternalKey == "new-key");
    }

    [Fact]
    public async Task GetReportAsync_returns_null_when_run_is_missing()
    {
        var repository = new InMemorySearchRunRepository(
            new SearchRunStorageOptions(),
            NullLogger<InMemorySearchRunRepository>.Instance);
        var history = new SearchRunHistoryService(repository, new SearchReportBuilder());

        var report = await history.GetReportAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.Null(report);
    }

    [Fact]
    public async Task GetRecentAsync_builds_recent_run_list_projection()
    {
        var repository = new InMemorySearchRunRepository(
            new SearchRunStorageOptions(),
            NullLogger<InMemorySearchRunRepository>.Instance);
        var history = new SearchRunHistoryService(repository, new SearchReportBuilder());
        var run = CreateRun(DateTimeOffset.UtcNow, CreateListing("key", "Alpha Law"));
        await repository.SaveAsync(run, CancellationToken.None);

        var recentRuns = await history.GetRecentAsync(CancellationToken.None);

        var recentRun = Assert.Single(recentRuns);
        Assert.Equal(run.Id, recentRun.RunId);
        Assert.Equal(1, recentRun.TotalListings);
        Assert.Equal(1, recentRun.LocationsSearched);
        Assert.Equal(0, recentRun.FailedLocations);
    }

    private static SearchRun CreateRun(DateTimeOffset completedAt, params SolicitorListing[] listings)
    {
        return new SearchRun(
            Guid.NewGuid(),
            completedAt.AddSeconds(-10),
            completedAt,
            ["London"],
            listings,
            []);
    }

    private static SolicitorListing CreateListing(string key, string name)
    {
        return SolicitorListing.Create(
            key,
            name,
            "London",
            ContactDetails.Create("1 Test Street", "020 0000 0000", "https://example.com", null, $"https://example.com/{key}"),
            null,
            Rating.Empty,
            [],
            false);
    }
}
