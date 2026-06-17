using InfoTrack.Application.Abstractions;
using InfoTrack.Application.LocationSearch;
using InfoTrack.Application.SearchRuns;
using InfoTrack.Application.SearchRuns.Reports;
using InfoTrack.Domain.Solicitors;
using InfoTrack.Infrastructure.Persistence;
using Microsoft.Extensions.Logging.Abstractions;

namespace InfoTrack.IntegrationTests;

public sealed class RunSolicitorSearchHandlerTests
{
    [Fact]
    public async Task HandleAsync_returns_partial_success_when_one_location_fails()
    {
        var repository = CreateRepository();
        var client = new FakeSolicitorSearchClient(new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "Leeds" });
        var handler = CreateHandler(client, repository);

        var report = await handler.HandleAsync(
            new RunSolicitorSearchRequest(["London", "Leeds"], CompareWithPreviousRun: true),
            CancellationToken.None);

        Assert.Equal(1, report.Summary.TotalListings);
        Assert.Equal(1, report.Summary.FailedLocations);
        Assert.Contains(report.Failures, failure => failure.Location == "Leeds");
        Assert.NotNull(await repository.GetAsync(report.RunId, CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_rejects_empty_locations()
    {
        var handler = CreateHandler(
            new FakeSolicitorSearchClient(new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
            CreateRepository());

        await Assert.ThrowsAsync<SearchValidationException>(() =>
            handler.HandleAsync(new RunSolicitorSearchRequest([], true), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_rejects_unsupported_location_characters()
    {
        var handler = CreateHandler(
            new FakeSolicitorSearchClient(new HashSet<string>(StringComparer.OrdinalIgnoreCase)),
            CreateRepository());

        await Assert.ThrowsAsync<SearchValidationException>(() =>
            handler.HandleAsync(new RunSolicitorSearchRequest(["London<script>"], true), CancellationToken.None));
    }

    [Fact]
    public async Task HandleAsync_does_not_convert_implementation_failures_to_location_failures()
    {
        var repository = CreateRepository();
        var handler = CreateHandler(new BrokenSolicitorSearchClient(), repository);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            handler.HandleAsync(new RunSolicitorSearchRequest(["London"], true), CancellationToken.None));

        var recentRuns = await repository.GetRecentAsync(10, CancellationToken.None);
        Assert.Empty(recentRuns);
    }

    [Fact]
    public async Task HandleAsync_compares_against_previous_in_memory_run()
    {
        var repository = CreateRepository();
        var client = new FakeSolicitorSearchClient(new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        var handler = CreateHandler(client, repository);

        var first = await handler.HandleAsync(new RunSolicitorSearchRequest(["London"], true), CancellationToken.None);
        client.AdditionalListing = CreateListing("new-london", "New London LLP", "London");
        var second = await handler.HandleAsync(new RunSolicitorSearchRequest(["London"], true), CancellationToken.None);

        Assert.Equal(1, first.Summary.NewListings);
        Assert.Equal(1, second.Summary.NewListings);
        Assert.Contains(second.NewListings, listing => listing.ExternalKey == "new-london");
    }

    private static SolicitorListing CreateListing(string key, string name, string location)
    {
        return SolicitorListing.Create(
            key,
            name,
            location,
            ContactDetails.Create("1 Test Street", "020 0000 0000", "https://example.com", null, $"https://example.com/{key}"),
            null,
            Rating.Create(4.5m, 25),
            [],
            true);
    }

    private static InMemorySearchRunRepository CreateRepository()
    {
        return new InMemorySearchRunRepository(
            new SearchRunStorageOptions(),
            NullLogger<InMemorySearchRunRepository>.Instance);
    }

    private static RunSolicitorSearchHandler CreateHandler(
        ISolicitorSearchClient client,
        InMemorySearchRunRepository repository)
    {
        return new RunSolicitorSearchHandler(
            new LocationSearchOrchestrator(client, NullLogger<LocationSearchOrchestrator>.Instance),
            repository,
            new SearchReportBuilder(),
            NullLogger<RunSolicitorSearchHandler>.Instance);
    }

    private sealed class FakeSolicitorSearchClient(IReadOnlySet<string> failLocations) : ISolicitorSearchClient
    {
        public SolicitorListing? AdditionalListing { get; set; }

        public Task<SolicitorSearchOutcome> SearchAsync(string location, CancellationToken cancellationToken)
        {
            if (failLocations.Contains(location))
            {
                return Task.FromResult(SolicitorSearchOutcome.UpstreamFailed(
                    location,
                    "Simulated upstream failure.",
                    SolicitorSearchUpstreamFailureReason.Network));
            }

            var listings = new List<SolicitorListing>
            {
                CreateListing($"{location.ToLowerInvariant()}-key", $"{location} Legal", location)
            };

            if (AdditionalListing is not null)
            {
                listings.Add(AdditionalListing);
            }

            return Task.FromResult(SolicitorSearchOutcome.Succeeded(
                new LocationSearchResult(location, "https://example.com", listings)));
        }
    }

    private sealed class BrokenSolicitorSearchClient : ISolicitorSearchClient
    {
        public Task<SolicitorSearchOutcome> SearchAsync(string location, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("Parser bug should not be reported as a location failure.");
        }
    }
}
