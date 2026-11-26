namespace SalesDistricts.Client.Api;

// The client's own copy of the API's JSON contract. It deliberately doesn't reference the API project.
public sealed record DistrictSummaryDto(int Id, string Name, string PrimarySalesperson);

public sealed record SalespersonDto(int Id, string Name);

public sealed record StoreDto(int Id, string Name);

public sealed record DistrictDetailDto(
    int Id,
    string Name,
    SalespersonDto Primary,
    IReadOnlyList<SalespersonDto> Secondaries,
    IReadOnlyList<StoreDto> Stores);

public sealed record ProblemDto(
    int? Status,
    string? Title,
    string? Detail,
    string? TraceId,
    Dictionary<string, string[]>? Errors);
