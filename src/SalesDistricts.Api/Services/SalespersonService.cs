using SalesDistricts.Api.Contracts;
using SalesDistricts.Api.Data;

namespace SalesDistricts.Api.Services;

public interface ISalespersonService
{
    Task<IReadOnlyList<SalespersonDto>> GetSalespeopleAsync(CancellationToken ct);
}

public sealed class SalespersonService(ISalespersonRepository repository) : ISalespersonService
{
    public async Task<IReadOnlyList<SalespersonDto>> GetSalespeopleAsync(CancellationToken ct)
    {
        var salespeople = await repository.GetAllAsync(ct);
        return [.. salespeople.Select(s => new SalespersonDto(s.SalespersonId, s.Name))];
    }
}
