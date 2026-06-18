using System.Threading.RateLimiting;
using InfoTrack.Api.Configuration;
using InfoTrack.Application.Abstractions;
using InfoTrack.Application.LocationSearch;
using InfoTrack.Application.SearchRuns;
using InfoTrack.Application.SearchRuns.History;
using InfoTrack.Application.SearchRuns.Reports;
using InfoTrack.Infrastructure.Persistence;
using InfoTrack.Infrastructure.Solicitors;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi;

namespace InfoTrack.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public const string ClientCorsPolicyName = "Client";

    public static IServiceCollection AddInfoTrackApi(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var options = configuration.GetInfoTrackOptions();

        services.AddSwagger();
        services.AddProblemDetails();
        services.AddClientCors(options.Cors);
        services.AddForwardedProxy(options.ForwardedProxy);
        services.AddApiRateLimiting(options.RateLimiting);
        services.AddSearchApplication();
        services.AddInfrastructure(options);

        return services;
    }

    private static void AddSwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "InfoTrack Solicitor Intelligence API",
                Version = "v1",
                Description = "Runs conveyancing solicitor searches and returns in-memory comparison reports."
            });
        });
    }

    private static void AddClientCors(this IServiceCollection services, ClientCorsOptions corsOptions)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(ClientCorsPolicyName, policy =>
            {
                policy
                    .WithOrigins(corsOptions.ClientOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });
    }

    private static void AddForwardedProxy(this IServiceCollection services, ForwardedProxyOptions proxyOptions)
    {
        services.AddSingleton(proxyOptions);

        if (!proxyOptions.Enabled)
        {
            return;
        }

        services.Configure<Microsoft.AspNetCore.Builder.ForwardedHeadersOptions>(options =>
        {
            options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;

            foreach (var knownNetwork in proxyOptions.KnownNetworks)
            {
                var (prefix, prefixLength) = ForwardedProxyOptions.ParseNetwork(knownNetwork);
                options.KnownIPNetworks.Add(new System.Net.IPNetwork(prefix, prefixLength));
            }

            foreach (var knownProxy in proxyOptions.KnownProxies)
            {
                options.KnownProxies.Add(System.Net.IPAddress.Parse(knownProxy));
            }
        });
    }

    private static void AddApiRateLimiting(
        this IServiceCollection services,
        ApiRateLimitingOptions rateLimitingOptions)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    context.Connection.RemoteIpAddress?.ToString() ?? "anonymous",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = rateLimitingOptions.PermitLimit,
                        Window = TimeSpan.FromSeconds(rateLimitingOptions.WindowSeconds),
                        QueueLimit = rateLimitingOptions.QueueLimit,
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                    }));
        });
    }

    private static void AddSearchApplication(this IServiceCollection services)
    {
        services.AddSingleton<SearchReportBuilder>();
        services.AddScoped<LocationSearchOrchestrator>();
        services.AddScoped<RunSolicitorSearchHandler>();
        services.AddScoped<SearchRunHistoryService>();
    }

    private static void AddInfrastructure(this IServiceCollection services, InfoTrackOptions options)
    {
        services.AddSingleton(options.SearchRunStorage);
        services.AddSingleton(options.SolicitorsClient);
        services.AddSingleton<ISearchRunRepository, InMemorySearchRunRepository>();
        services.AddSingleton<ManualSolicitorsHtmlParser>();
        services.AddHttpClient<ISolicitorSearchClient, SolicitorsHttpClient>((serviceProvider, client) =>
        {
            var clientOptions = serviceProvider.GetRequiredService<SolicitorsClientOptions>();
            client.BaseAddress = clientOptions.GetBaseAddress();
            client.DefaultRequestHeaders.UserAgent.ParseAdd(clientOptions.UserAgent);
            client.Timeout = TimeSpan.FromSeconds(clientOptions.TimeoutSeconds);
        });
    }
}
