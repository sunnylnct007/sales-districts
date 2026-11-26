namespace SalesDistricts.Api.Contracts;

public sealed record DistrictDetailDto(
    int Id,
    string Name,
    SalespersonDto Primary,
    IReadOnlyList<SalespersonDto> Secondaries,
    IReadOnlyList<StoreDto> Stores);

public sealed record SalespersonDto(int Id, string Name);

public sealed record StoreDto(int Id, string Name);
