using System.Net;
using SalesDistricts.Client.Api;

namespace SalesDistricts.Client.Tests;

public sealed class DistrictApiClientTests
{
    private static CancellationToken Ct => TestContext.Current.CancellationToken;

    private static DistrictApiClient CreateClient(StubHttpHandler handler) =>
        new(new HttpClient(handler) { BaseAddress = new Uri("https://api.test/") });

    [Fact]
    public async Task GetDistrictsAsync_reads_the_list()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK,
            """[{ "id": 5, "name": "Capital Region", "primarySalesperson": "Carl Hansen" }]""");

        var districts = await CreateClient(handler).GetDistrictsAsync(Ct);

        Assert.Equal([new DistrictSummaryDto(5, "Capital Region", "Carl Hansen")], districts);
        Assert.Equal("https://api.test/api/districts", handler.Request!.RequestUri!.ToString());
    }

    [Fact]
    public async Task AddSecondaryAsync_posts_the_salesperson_and_reads_the_updated_district()
    {
        var handler = new StubHttpHandler(HttpStatusCode.OK, """
            { "id": 4, "name": "Zealand", "primary": { "id": 6, "name": "Freja Sørensen" },
              "secondaries": [{ "id": 7, "name": "Gustav Madsen" }], "stores": [] }
            """);

        var district = await CreateClient(handler).AddSecondaryAsync(4, 7, Ct);

        Assert.Equal(HttpMethod.Post, handler.Request!.Method);
        Assert.Equal("https://api.test/api/districts/4/secondaries", handler.Request.RequestUri!.ToString());
        Assert.Equal("""{"salespersonId":7}""", handler.RequestBody);
        Assert.Equal([new SalespersonDto(7, "Gustav Madsen")], district.Secondaries);
    }

    [Fact]
    public async Task Problem_details_become_an_api_exception_with_the_reason()
    {
        var handler = new StubHttpHandler(HttpStatusCode.Conflict,
            """{ "status": 409, "title": "Conflict", "detail": "Salesperson is already the primary of this district.", "traceId": "00-abc" }""",
            "application/problem+json");

        var ex = await Assert.ThrowsAsync<ApiException>(() => CreateClient(handler).AddSecondaryAsync(1, 1, Ct));

        Assert.Equal(HttpStatusCode.Conflict, ex.StatusCode);
        Assert.Equal("Salesperson is already the primary of this district.", ex.Message);
        Assert.Equal("00-abc", ex.TraceId);
    }

    [Fact]
    public async Task Validation_errors_are_turned_into_a_readable_message()
    {
        var handler = new StubHttpHandler(HttpStatusCode.BadRequest,
            """{ "status": 400, "title": "One or more validation errors occurred.", "errors": { "SalespersonId": ["The field SalespersonId must be between 1 and 2147483647."] } }""",
            "application/problem+json");

        var ex = await Assert.ThrowsAsync<ApiException>(() => CreateClient(handler).ReplacePrimaryAsync(3, 0, Ct));

        Assert.Contains("SalespersonId must be between", ex.Message);
    }

    [Fact]
    public async Task An_error_without_a_body_still_gives_a_message()
    {
        var handler = new StubHttpHandler(HttpStatusCode.Unauthorized);

        var ex = await Assert.ThrowsAsync<ApiException>(() => CreateClient(handler).GetDistrictsAsync(Ct));

        Assert.Equal(HttpStatusCode.Unauthorized, ex.StatusCode);
        Assert.Contains("401", ex.Message);
    }
}
