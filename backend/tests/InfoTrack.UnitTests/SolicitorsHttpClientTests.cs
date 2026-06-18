using System.Net;
using InfoTrack.Application.LocationSearch;
using InfoTrack.Infrastructure.Solicitors;
using Microsoft.Extensions.Logging.Abstractions;

namespace InfoTrack.UnitTests;

public sealed class SolicitorsHttpClientTests
{
    [Fact]
    public async Task SearchAsync_returns_user_safe_failure_for_server_errors()
    {
        using var httpClient = new HttpClient(new StaticResponseHandler(
            new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                ReasonPhrase = "Server Error"
            }))
        {
            BaseAddress = new Uri("https://www.solicitors.com")
        };

        var client = new SolicitorsHttpClient(
            httpClient,
            new ManualSolicitorsHtmlParser(new SolicitorsClientOptions()),
            new SolicitorsClientOptions(),
            NullLogger<SolicitorsHttpClient>.Instance);

        var outcome = await client.SearchAsync("London", CancellationToken.None);

        var failure = Assert.IsType<SolicitorSearchUpstreamFailure>(outcome);
        Assert.Equal("London", failure.Location);
        Assert.Equal(SolicitorSearchUpstreamFailureReason.HttpStatus, failure.Reason);
        Assert.Equal("The search provider is temporarily unavailable. Try again later.", failure.Error);
    }

    [Fact]
    public async Task SearchAsync_returns_user_safe_failure_for_redirects()
    {
        using var httpClient = new HttpClient(new StaticResponseHandler(
            new HttpResponseMessage(HttpStatusCode.MovedPermanently)
            {
                ReasonPhrase = "Moved Permanently"
            }))
        {
            BaseAddress = new Uri("https://www.solicitors.com")
        };

        var client = new SolicitorsHttpClient(
            httpClient,
            new ManualSolicitorsHtmlParser(new SolicitorsClientOptions()),
            new SolicitorsClientOptions(),
            NullLogger<SolicitorsHttpClient>.Instance);

        var outcome = await client.SearchAsync("Baku", CancellationToken.None);

        var failure = Assert.IsType<SolicitorSearchUpstreamFailure>(outcome);
        Assert.Equal("Baku", failure.Location);
        Assert.Equal(SolicitorSearchUpstreamFailureReason.HttpStatus, failure.Reason);
        Assert.Equal(
            "No results were returned for this location. Check that it is a supported UK town or city.",
            failure.Error);
    }

    [Fact]
    public async Task SearchAsync_returns_provider_format_failure_for_unexpected_source_data()
    {
        const string html = """
        <div class="result-item">
          <span class="h2">Too Many Stars<span class="rev-results">
            <div class="star-full rating-lrg"></div>
            <div class="star-full rating-lrg"></div>
            <div class="star-full rating-lrg"></div>
            <div class="star-full rating-lrg"></div>
            <div class="star-full rating-lrg"></div>
            <div class="star-full rating-lrg"></div>
            (12)
          </span></span>
          <a href="/too-many-stars.html" class="link-map"><i></i><address>London</address></a>
        </div>
        """;

        using var httpClient = new HttpClient(new StaticResponseHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(html)
            }))
        {
            BaseAddress = new Uri("https://www.solicitors.com")
        };

        var options = new SolicitorsClientOptions();
        var client = new SolicitorsHttpClient(
            httpClient,
            new ManualSolicitorsHtmlParser(options),
            options,
            NullLogger<SolicitorsHttpClient>.Instance);

        var outcome = await client.SearchAsync("London", CancellationToken.None);

        var failure = Assert.IsType<SolicitorSearchUpstreamFailure>(outcome);
        Assert.Equal(SolicitorSearchUpstreamFailureReason.ProviderFormat, failure.Reason);
        Assert.Equal("The search provider returned data in an unexpected format. Try again later.", failure.Error);
    }

    [Fact]
    public async Task SearchAsync_returns_response_too_large_failure_when_html_exceeds_limit()
    {
        var options = new SolicitorsClientOptions { MaxResponseBytes = 16 };
        using var httpClient = new HttpClient(new StaticResponseHandler(
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(new string('x', 64))
            }))
        {
            BaseAddress = new Uri("https://www.solicitors.com")
        };

        var client = new SolicitorsHttpClient(
            httpClient,
            new ManualSolicitorsHtmlParser(options),
            options,
            NullLogger<SolicitorsHttpClient>.Instance);

        var outcome = await client.SearchAsync("London", CancellationToken.None);

        var failure = Assert.IsType<SolicitorSearchUpstreamFailure>(outcome);
        Assert.Equal(SolicitorSearchUpstreamFailureReason.ResponseTooLarge, failure.Reason);
        Assert.Equal("The search provider returned more data than expected. Try again later.", failure.Error);
    }

    private sealed class StaticResponseHandler(HttpResponseMessage response) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            response.RequestMessage = request;
            return Task.FromResult(response);
        }
    }
}
