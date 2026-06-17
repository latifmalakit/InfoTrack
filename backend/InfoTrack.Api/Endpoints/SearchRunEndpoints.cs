using InfoTrack.Api.Contracts;
using InfoTrack.Application.SearchRuns;
using InfoTrack.Application.SearchRuns.History;
using InfoTrack.Application.SearchRuns.Reports;

namespace InfoTrack.Api.Endpoints;

public static class SearchRunEndpoints
{
    public static IEndpointRouteBuilder MapSearchRunEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/search-runs")
            .WithTags("SearchRuns");

        group.MapPost("/", CreateSearchRunAsync)
            .WithName("CreateSearchRun")
            .Accepts<SearchRunRequestDto>("application/json")
            .Produces<SearchRunReport>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/recent", GetRecentSearchRunsAsync)
            .WithName("GetRecentSearchRuns")
            .Produces<IReadOnlyList<SearchRunListItem>>();

        group.MapGet("/{runId:guid}", GetSearchRunAsync)
            .WithName("GetSearchRun")
            .Produces<SearchRunReport>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        return endpoints;
    }

    private static async Task<IResult> CreateSearchRunAsync(
        SearchRunRequestDto request,
        RunSolicitorSearchHandler handler,
        CancellationToken cancellationToken)
    {
        var report = await handler.HandleAsync(
            new RunSolicitorSearchRequest(request.Locations, request.CompareWithPreviousRun),
            cancellationToken);

        return Results.Ok(report);
    }

    private static async Task<IResult> GetRecentSearchRunsAsync(
        SearchRunHistoryService history,
        CancellationToken cancellationToken)
    {
        var runs = await history.GetRecentAsync(cancellationToken);
        return Results.Ok(runs);
    }

    private static async Task<IResult> GetSearchRunAsync(
        Guid runId,
        SearchRunHistoryService history,
        CancellationToken cancellationToken)
    {
        var report = await history.GetReportAsync(runId, cancellationToken);
        if (report is null)
        {
            return Results.Problem(
                title: "Search run not found",
                detail: $"No search run exists with id '{runId}'.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Results.Ok(report);
    }
}
