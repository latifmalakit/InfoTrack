using InfoTrack.Infrastructure.Persistence;
using InfoTrack.Infrastructure.Solicitors;

namespace InfoTrack.Api.Configuration;

public sealed record InfoTrackOptions(
    ClientCorsOptions Cors,
    ApiRateLimitingOptions RateLimiting,
    SolicitorsClientOptions SolicitorsClient,
    SearchRunStorageOptions SearchRunStorage);

public static class ConfigurationExtensions
{
    public static InfoTrackOptions GetInfoTrackOptions(this IConfiguration configuration)
    {
        return new InfoTrackOptions(
            GetValidatedOptions(
                configuration,
                ClientCorsOptions.SectionName,
                static () => new ClientCorsOptions(),
                static options => options.Validate()),
            GetValidatedOptions(
                configuration,
                ApiRateLimitingOptions.SectionName,
                static () => new ApiRateLimitingOptions(),
                static options => options.Validate()),
            GetValidatedOptions(
                configuration,
                SolicitorsClientOptions.SectionName,
                static () => new SolicitorsClientOptions(),
                static options => options.Validate()),
            GetValidatedOptions(
                configuration,
                SearchRunStorageOptions.SectionName,
                static () => new SearchRunStorageOptions(),
                static options => options.Validate()));
    }

    private static TOptions GetValidatedOptions<TOptions>(
        IConfiguration configuration,
        string sectionName,
        Func<TOptions> createDefault,
        Action<TOptions> validate)
    {
        var options = configuration.GetSection(sectionName).Get<TOptions>() ?? createDefault();
        validate(options);
        return options;
    }
}
