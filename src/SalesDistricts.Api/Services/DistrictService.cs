using SalesDistricts.Api.Contracts;
using SalesDistricts.Api.Data;

namespace SalesDistricts.Api.Services;

public interface IDistrictService
{
    Task<IReadOnlyList<DistrictSummaryDto>> GetDistrictsAsync(CancellationToken ct);
    Task<DistrictDetailDto?> GetDistrictAsync(int districtId, CancellationToken ct);
    Task<AssignmentResult> AddSecondaryAsync(int districtId, int salespersonId, CancellationToken ct);
    Task<AssignmentResult> RemoveSecondaryAsync(int districtId, int salespersonId, CancellationToken ct);
    Task<AssignmentResult> ReplacePrimaryAsync(int districtId, int salespersonId, CancellationToken ct);
}

public sealed class DistrictService(
    IDistrictRepository repository, ISalespersonRepository salespeople, ILogger<DistrictService> logger) : IDistrictService
{
    public async Task<IReadOnlyList<DistrictSummaryDto>> GetDistrictsAsync(CancellationToken ct)
    {
        var districts = await repository.GetAllAsync(ct);
        return [.. districts.Select(d => new DistrictSummaryDto(d.DistrictId, d.Name, d.PrimarySalespersonName))];
    }

    public async Task<DistrictDetailDto?> GetDistrictAsync(int districtId, CancellationToken ct) =>
        await repository.GetDetailAsync(districtId, ct) is { } district
            ? ToDto(district)
            : null;

    public async Task<AssignmentResult> AddSecondaryAsync(int districtId, int salespersonId, CancellationToken ct)
    {
        if (await repository.GetDetailAsync(districtId, ct) is not { } district)
            return new AssignmentResult.NotFound($"District {districtId} was not found.");

        if (!await salespeople.ExistsAsync(salespersonId, ct))
            return new AssignmentResult.Invalid($"Salesperson {salespersonId} does not exist.");

        if (district.Primary.SalespersonId == salespersonId)
            return new AssignmentResult.Conflict("Salesperson is already the primary of this district.");

        if (IsSecondary(district, salespersonId))
            return new AssignmentResult.Conflict("Salesperson is already a secondary in this district.");

        await repository.AddSecondaryAsync(districtId, salespersonId, ct);
        logger.LogInformation("Added salesperson {SalespersonId} as a secondary in district {DistrictId}", salespersonId, districtId);
        return await Updated(districtId, ct);
    }

    public async Task<AssignmentResult> RemoveSecondaryAsync(int districtId, int salespersonId, CancellationToken ct)
    {
        if (await repository.GetDetailAsync(districtId, ct) is not { } district)
            return new AssignmentResult.NotFound($"District {districtId} was not found.");

        if (!IsSecondary(district, salespersonId))
            return new AssignmentResult.NotFound("Salesperson is not a secondary in this district.");

        await repository.RemoveSecondaryAsync(districtId, salespersonId, ct);
        logger.LogInformation("Removed secondary salesperson {SalespersonId} from district {DistrictId}", salespersonId, districtId);
        return await Updated(districtId, ct);
    }

    // The previous primary leaves the district; a current secondary is promoted.
    public async Task<AssignmentResult> ReplacePrimaryAsync(int districtId, int salespersonId, CancellationToken ct)
    {
        if (await repository.GetDetailAsync(districtId, ct) is not { } district)
            return new AssignmentResult.NotFound($"District {districtId} was not found.");

        if (!await salespeople.ExistsAsync(salespersonId, ct))
            return new AssignmentResult.Invalid($"Salesperson {salespersonId} does not exist.");

        if (district.Primary.SalespersonId == salespersonId)
            return new AssignmentResult.Success(ToDto(district));   // already the primary: nothing to do

        await repository.ReplacePrimaryAsync(districtId, salespersonId, ct);
        logger.LogInformation("Replaced primary of district {DistrictId}: {PreviousPrimaryId} -> {NewPrimaryId}",
            districtId, district.Primary.SalespersonId, salespersonId);
        return await Updated(districtId, ct);
    }

    private async Task<AssignmentResult> Updated(int districtId, CancellationToken ct) =>
        await repository.GetDetailAsync(districtId, ct) is { } district
            ? new AssignmentResult.Success(ToDto(district))
            : new AssignmentResult.NotFound($"District {districtId} was not found.");

    private static bool IsSecondary(DistrictDetail district, int salespersonId) =>
        district.Secondaries.Any(s => s.SalespersonId == salespersonId);

    private static DistrictDetailDto ToDto(DistrictDetail district) => new(
        district.DistrictId,
        district.Name,
        ToDto(district.Primary),
        [.. district.Secondaries.Select(ToDto)],
        [.. district.Stores.Select(s => new StoreDto(s.StoreId, s.Name))]);

    private static SalespersonDto ToDto(SalespersonRecord salesperson) => new(salesperson.SalespersonId, salesperson.Name);

}
