using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SalesDistricts.Api.Contracts;

namespace SalesDistricts.Api.IntegrationTests;

// Writes use Zealand (4) and Southern Denmark (3), which the read tests don't inspect,
// and each test puts the data back the way it found it.
public sealed class AssignmentEndpointTests(ApiFactory api) : IClassFixture<ApiFactory>
{
    private const int NorthDenmark = 1, SouthernDenmark = 3, Zealand = 4, Anna = 1, Freja = 6, Gustav = 7;

    private readonly HttpClient _client = api.CreateClient();
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Secondary_can_be_added_and_removed()
    {
        var added = await _client.PostAsJsonAsync($"/api/districts/{Zealand}/secondaries", new AddSecondaryRequest(Gustav), Ct);
        Assert.Equal(HttpStatusCode.OK, added.StatusCode);
        var district = await added.Content.ReadFromJsonAsync<DistrictDetailDto>(Ct);
        Assert.Contains(district!.Secondaries, s => s.Id == Gustav);

        var removed = await _client.DeleteAsync($"/api/districts/{Zealand}/secondaries/{Gustav}", Ct);
        Assert.Equal(HttpStatusCode.OK, removed.StatusCode);
        district = await removed.Content.ReadFromJsonAsync<DistrictDetailDto>(Ct);
        Assert.DoesNotContain(district!.Secondaries, s => s.Id == Gustav);
    }

    [Fact]
    public async Task Adding_the_primary_as_a_secondary_returns_409_with_a_reason()
    {
        var response = await _client.PostAsJsonAsync($"/api/districts/{NorthDenmark}/secondaries", new AddSecondaryRequest(Anna), Ct);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(Ct);
        Assert.Contains("already the primary", problem!.Detail);
    }

    [Theory]
    [InlineData(999, Gustav, HttpStatusCode.NotFound)]                  // unknown district
    [InlineData(SouthernDenmark, 999, HttpStatusCode.UnprocessableEntity)] // unknown salesperson
    [InlineData(SouthernDenmark, 0, HttpStatusCode.BadRequest)]            // invalid request
    public async Task Adding_a_secondary_validates_the_request(int districtId, int salespersonId, HttpStatusCode expected)
    {
        var response = await _client.PostAsJsonAsync($"/api/districts/{districtId}/secondaries", new AddSecondaryRequest(salespersonId), Ct);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Removing_someone_who_is_not_a_secondary_returns_404()
    {
        var response = await _client.DeleteAsync($"/api/districts/{SouthernDenmark}/secondaries/{Gustav}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Primary_can_be_replaced_and_the_previous_primary_leaves_the_district()
    {
        var district = await ReplacePrimary(Zealand, Gustav);

        Assert.Equal(Gustav, district.Primary.Id);
        Assert.DoesNotContain(district.Secondaries, s => s.Id == Freja);

        await ReplacePrimary(Zealand, Freja);   // put the seed data back
    }

    [Fact]
    public async Task Secondary_can_be_promoted_to_primary()
    {
        await _client.PostAsJsonAsync($"/api/districts/{Zealand}/secondaries", new AddSecondaryRequest(Gustav), Ct);

        var district = await ReplacePrimary(Zealand, Gustav);

        Assert.Equal(Gustav, district.Primary.Id);
        Assert.DoesNotContain(district.Secondaries, s => s.Id == Gustav);

        await ReplacePrimary(Zealand, Freja);
    }

    [Fact]
    public async Task Setting_the_current_primary_again_changes_nothing()
    {
        var district = await ReplacePrimary(NorthDenmark, Anna);

        Assert.Equal(Anna, district.Primary.Id);
    }

    [Theory]
    [InlineData(999, Gustav, HttpStatusCode.NotFound)]
    [InlineData(SouthernDenmark, 999, HttpStatusCode.UnprocessableEntity)]
    [InlineData(SouthernDenmark, 0, HttpStatusCode.BadRequest)]
    public async Task Replacing_the_primary_validates_the_request(int districtId, int salespersonId, HttpStatusCode expected)
    {
        var response = await _client.PutAsJsonAsync($"/api/districts/{districtId}/primary", new ReplacePrimaryRequest(salespersonId), Ct);

        Assert.Equal(expected, response.StatusCode);
    }

    private async Task<DistrictDetailDto> ReplacePrimary(int districtId, int salespersonId)
    {
        var response = await _client.PutAsJsonAsync($"/api/districts/{districtId}/primary", new ReplacePrimaryRequest(salespersonId), Ct);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<DistrictDetailDto>(Ct))!;
    }
}
