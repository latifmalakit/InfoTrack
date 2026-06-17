using InfoTrack.Api.Contracts;
using InfoTrack.Application.Locations;

namespace InfoTrack.Api.Endpoints;

public static class LocationEndpoints
{
    public static IEndpointRouteBuilder MapLocationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/locations")
            .WithTags("Locations");

        group.MapGet("/defaults", () => Results.Ok(new DefaultLocationsResponse(DefaultLocations.Values)))
            .WithName("GetDefaultLocations")
            .Produces<DefaultLocationsResponse>();

        return endpoints;
    }
}
