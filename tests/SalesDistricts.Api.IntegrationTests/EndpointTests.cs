using System.Net;
using System.Net.Http.Json;
using SalesDistricts.Api.Contracts;

namespace SalesDistricts.Api.IntegrationTests;

public sealed class EndpointTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private readonly HttpClient _client = api.CreateClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Get_districts_returns_all_districts_sorted_by_name()
    {
        var districts = await _client.GetFromJsonAsync<List<DistrictSummaryDto>>("/api/districts", Ct);

        Assert.Equal(5, districts!.Count);
        Assert.Equal(new DistrictSummaryDto(5, "Capital Region", "Carl Hansen"), districts[0]);
    }

    [Fact]
    public async Task Get_district_returns_its_primary_secondaries_and_stores()
    {
        var district = await _client.GetFromJsonAsync<DistrictDetailDto>("/api/districts/5", Ct);

        Assert.Equal("Capital Region", district!.Name);
        Assert.Equal(new SalespersonDto(3, "Carl Hansen"), district.Primary);
        Assert.Equal(["Bo Nielsen", "Dorte Pedersen"], district.Secondaries.Select(s => s.Name));
        Assert.Equal(["Copenhagen Store", "Hillerød Store"], district.Stores.Select(s => s.Name));
    }

    [Fact]
    public async Task Get_unknown_district_returns_404()
    {
        var response = await _client.GetAsync("/api/districts/999", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Get_salespeople_returns_everyone_sorted_by_name()
    {
        var salespeople = await _client.GetFromJsonAsync<List<SalespersonDto>>("/api/salespeople", Ct);

        Assert.Equal(7, salespeople!.Count);
        Assert.Equal(new SalespersonDto(1, "Anna Jensen"), salespeople[0]);
    }
}
