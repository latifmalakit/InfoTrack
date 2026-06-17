using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using InfoTrack.Application.Abstractions;
using InfoTrack.Application.LocationSearch;
using InfoTrack.Application.SearchRuns.Reports;
using InfoTrack.Domain.Solicitors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace InfoTrack.IntegrationTests;

public sealed class ApiEndpointTests
{
    [Fact]
    public async Task Search_runs_create_recent_and_get_by_id_use_minimal_api_endpoints()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var createResponse = await client.PostAsJsonAsync(
            "/api/search-runs",
            new { locations = new[] { "London" }, compareWithPreviousRun = true });

        createResponse.EnsureSuccessStatusCode();
        var createdReport = await createResponse.Content.ReadFromJsonAsync<SearchRunReport>();
        Assert.NotNull(createdReport);
        Assert.Equal(1, createdReport.Summary.TotalListings);

        var recentRuns = await client.GetFromJsonAsync<List<SearchRunListItem>>("/api/search-runs/recent");
        Assert.NotNull(recentRuns);
        Assert.Contains(recentRuns, run => run.RunId == createdReport.RunId);

        var savedReport = await client.GetFromJsonAsync<SearchRunReport>($"/api/search-runs/{createdReport.RunId}");
        Assert.NotNull(savedReport);
        Assert.Equal(createdReport.RunId, savedReport.RunId);
        Assert.Equal(createdReport.Summary.TotalListings, savedReport.Summary.TotalListings);
    }

    [Fact]
    public async Task Search_runs_create_returns_problem_details_from_central_exception_handler()
    {
        await using var factory = CreateFactory();
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync(
            "/api/search-runs",
            new { locations = new[] { "London<script>" }, compareWithPreviousRun = true });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var payload = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.Equal("Invalid search request", payload.RootElement.GetProperty("title").GetString());
        Assert.Equal(400, payload.RootElement.GetProperty("status").GetInt32());
    }

    private static WebApplicationFactory<Program> CreateFactory()
    {
        return new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    services.RemoveAll<ISolicitorSearchClient>();
                    services.AddSingleton<ISolicitorSearchClient, FakeSolicitorSearchClient>();
                });
            });
    }

    private sealed class FakeSolicitorSearchClient : ISolicitorSearchClient
    {
        public Task<SolicitorSearchOutcome> SearchAsync(string location, CancellationToken cancellationToken)
        {
            IReadOnlyList<SolicitorListing> listings =
            [
                SolicitorListing.Create(
                    $"{location.ToLowerInvariant()}-api-key",
                    $"{location} Legal",
                    location,
                    ContactDetails.Create(
                        "1 Test Street",
                        "020 0000 0000",
                        "https://example.com",
                        null,
                        $"https://example.com/{location.ToLowerInvariant()}"),
                    null,
                    Rating.Create(4.5m, 25),
                    [],
                    true)
            ];

            return Task.FromResult(SolicitorSearchOutcome.Succeeded(
                new LocationSearchResult(location, "https://example.com", listings)));
        }
    }
}
