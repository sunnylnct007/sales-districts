namespace SalesDistricts.Api.Data;

public sealed record DistrictSummary(int DistrictId, string Name, string PrimarySalespersonName);

public sealed record SalespersonRecord(int SalespersonId, string Name);

public sealed record StoreRecord(int StoreId, string Name);

public sealed record DistrictDetail(
    int DistrictId,
    string Name,
    SalespersonRecord Primary,
    IReadOnlyList<SalespersonRecord> Secondaries,
    IReadOnlyList<StoreRecord> Stores);
