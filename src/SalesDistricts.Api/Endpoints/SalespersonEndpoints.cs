using Microsoft.AspNetCore.Http.HttpResults;
using SalesDistricts.Api.Authorization;
using SalesDistricts.Api.Contracts;
using SalesDistricts.Api.Services;

namespace SalesDistricts.Api.Endpoints;

public static class SalespersonEndpoints
{
    public static void MapSalespersonEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGroup("/api/salespeople")
            .WithTags("Salespeople")
            .RequireAuthorization(Policies.CanViewDistricts)
            .MapGet("/", GetSalespeople);
    }

    private static async Task<Ok<IReadOnlyList<SalespersonDto>>> GetSalespeople(
        ISalespersonService service, CancellationToken ct) =>
        TypedResults.Ok(await service.GetSalespeopleAsync(ct));
}
