using InfoTrack.Api.Endpoints;
using InfoTrack.Api.ErrorHandling;
using InfoTrack.Api.Security;

namespace InfoTrack.Api.Extensions;

public static class WebApplicationExtensions
{
    public static WebApplication UseInfoTrackApi(this WebApplication app)
    {
        app.UseMiddleware<ApiExceptionHandlingMiddleware>();
        app.UseSecurityHeaders();
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.SwaggerEndpoint("/swagger/v1/swagger.json", "InfoTrack Solicitor Intelligence API v1");
            options.DocumentTitle = "InfoTrack API";
        });
        app.UseCors(ServiceCollectionExtensions.ClientCorsPolicyName);
        app.UseRateLimiter();

        app.MapLocationEndpoints();
        app.MapSearchRunEndpoints();
        app.MapGet("/health", () => Results.Ok(new { status = "ok" }))
            .WithName("Health")
            .WithTags("Health");

        return app;
    }
}
