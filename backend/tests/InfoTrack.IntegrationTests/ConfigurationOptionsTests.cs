using InfoTrack.Api.Configuration;

namespace InfoTrack.IntegrationTests;

public sealed class ConfigurationOptionsTests
{
    [Theory]
    [InlineData("https://example.com/")]
    [InlineData("https://example.com/app")]
    [InlineData("https://example.com?query=1")]
    [InlineData("https://user@example.com")]
    public void ClientCorsOptions_rejects_values_that_are_not_origins(string origin)
    {
        var options = new ClientCorsOptions { ClientOrigins = [origin] };

        Assert.Throws<InvalidOperationException>(options.Validate);
    }

    [Fact]
    public void ClientCorsOptions_accepts_scheme_host_and_optional_port()
    {
        var options = new ClientCorsOptions { ClientOrigins = ["https://example.com", "http://localhost:5173"] };

        options.Validate();
    }
}
