using System.Diagnostics;
using Microsoft.AspNetCore.Http.HttpResults;
using SalesDistricts.Api.Authorization;
using SalesDistricts.Api.Contracts;
using SalesDistricts.Api.Services;

namespace SalesDistricts.Api.Endpoints;

public static class DistrictEndpoints
{
    public static void MapDistrictEndpoints(this IEndpointRouteBuilder app)
    {
        var districts = app.MapGroup("/api/districts")
            .WithTags("Districts")
            .RequireAuthorization(Policies.CanViewDistricts);

        districts.MapGet("/", GetDistricts);
        districts.MapGet("/{id:int}", GetDistrict);

        districts.MapPost("/{id:int}/secondaries", AddSecondary)
            .RequireAuthorization(Policies.CanEditAssignments)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
        districts.MapDelete("/{id:int}/secondaries/{salespersonId:int}", RemoveSecondary)
            .RequireAuthorization(Policies.CanEditAssignments)
            .ProducesProblem(StatusCodes.Status404NotFound);
        districts.MapPut("/{id:int}/primary", ReplacePrimary)
            .RequireAuthorization(Policies.CanEditAssignments)
            .ProducesValidationProblem()
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);
    }

    private static async Task<Ok<IReadOnlyList<DistrictSummaryDto>>> GetDistricts(
        IDistrictService service, CancellationToken ct) =>
        TypedResults.Ok(await service.GetDistrictsAsync(ct));

    private static async Task<Results<Ok<DistrictDetailDto>, NotFound>> GetDistrict(
        int id, IDistrictService service, CancellationToken ct) =>
        await service.GetDistrictAsync(id, ct) is { } district
            ? TypedResults.Ok(district)
            : TypedResults.NotFound();

    private static async Task<Results<Ok<DistrictDetailDto>, ProblemHttpResult>> AddSecondary(
        int id, AddSecondaryRequest request, IDistrictService service, CancellationToken ct) =>
        ToHttp(await service.AddSecondaryAsync(id, request.SalespersonId, ct));

    private static async Task<Results<Ok<DistrictDetailDto>, ProblemHttpResult>> RemoveSecondary(
        int id, int salespersonId, IDistrictService service, CancellationToken ct) =>
        ToHttp(await service.RemoveSecondaryAsync(id, salespersonId, ct));

    private static async Task<Results<Ok<DistrictDetailDto>, ProblemHttpResult>> ReplacePrimary(
        int id, ReplacePrimaryRequest request, IDistrictService service, CancellationToken ct) =>
        ToHttp(await service.ReplacePrimaryAsync(id, request.SalespersonId, ct));

    private static Results<Ok<DistrictDetailDto>, ProblemHttpResult> ToHttp(AssignmentResult result) => result switch
    {
        AssignmentResult.Success success => TypedResults.Ok(success.District),
        AssignmentResult.NotFound notFound => TypedResults.Problem(notFound.Reason, statusCode: StatusCodes.Status404NotFound),
        AssignmentResult.Invalid invalid => TypedResults.Problem(invalid.Reason, statusCode: StatusCodes.Status422UnprocessableEntity),
        AssignmentResult.Conflict conflict => TypedResults.Problem(conflict.Reason, statusCode: StatusCodes.Status409Conflict),
        _ => throw new UnreachableException()
    };
}
