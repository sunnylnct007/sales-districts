using System.Net;
using System.Net.Http.Json;
using SalesDistricts.Api.Contracts;

namespace SalesDistricts.Api.IntegrationTests;

public sealed class AuthenticationTests(AuthenticatedApiFactory api) : IClassFixture<AuthenticatedApiFactory>
{
    [Theory]
    [InlineData("/api/districts")]
    [InlineData("/api/districts/5")]
    [InlineData("/api/salespeople")]
    public async Task Endpoints_require_a_token_when_authentication_is_enabled(string url)
    {
        var response = await api.CreateClient().GetAsync(url, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Changes_require_a_token_when_authentication_is_enabled()
    {
        var response = await api.CreateClient().PostAsJsonAsync(
            "/api/districts/4/secondaries", new AddSecondaryRequest(7), TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
