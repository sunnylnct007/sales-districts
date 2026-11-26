using Dapper;
using SalesDistricts.Api.Database;

namespace SalesDistricts.Api.Data;

public interface IDistrictRepository
{
    Task<IReadOnlyList<DistrictSummary>> GetAllAsync(CancellationToken ct);
    Task<DistrictDetail?> GetDetailAsync(int districtId, CancellationToken ct);
    Task AddSecondaryAsync(int districtId, int salespersonId, CancellationToken ct);
    Task RemoveSecondaryAsync(int districtId, int salespersonId, CancellationToken ct);
    Task ReplacePrimaryAsync(int districtId, int salespersonId, CancellationToken ct);
}

public sealed class DistrictRepository(IDbConnectionFactory connectionFactory) : IDistrictRepository
{
    public async Task<IReadOnlyList<DistrictSummary>> GetAllAsync(CancellationToken ct)
    {
        const string sql = """
            SELECT d.DistrictId, d.Name, s.Name AS PrimarySalespersonName
            FROM dbo.District d
            JOIN dbo.Salesperson s ON s.SalespersonId = d.PrimarySalespersonId
            ORDER BY d.Name;
            """;

        await using var connection = connectionFactory.Create();
        var districts = await connection.QueryAsync<DistrictSummary>(new CommandDefinition(sql, cancellationToken: ct));
        return districts.AsList();
    }

    public async Task<DistrictDetail?> GetDetailAsync(int districtId, CancellationToken ct)
    {
        const string sql = """
            SELECT d.DistrictId, d.Name, s.SalespersonId, s.Name AS SalespersonName
            FROM dbo.District d
            JOIN dbo.Salesperson s ON s.SalespersonId = d.PrimarySalespersonId
            WHERE d.DistrictId = @DistrictId;

            SELECT s.SalespersonId, s.Name
            FROM dbo.DistrictSalesperson ds
            JOIN dbo.Salesperson s ON s.SalespersonId = ds.SalespersonId
            WHERE ds.DistrictId = @DistrictId AND ds.Role = N'Secondary'
            ORDER BY s.Name;

            SELECT StoreId, Name
            FROM dbo.Store
            WHERE DistrictId = @DistrictId
            ORDER BY Name;
            """;

        await using var connection = connectionFactory.Create();
        await using var results = await connection.QueryMultipleAsync(
            new CommandDefinition(sql, new { DistrictId = districtId }, cancellationToken: ct));

        var district = await results.ReadSingleOrDefaultAsync<DistrictRow>();
        if (district is null)
            return null;

        var secondaries = (await results.ReadAsync<SalespersonRecord>()).AsList();
        var stores = (await results.ReadAsync<StoreRecord>()).AsList();

        return new DistrictDetail(
            district.DistrictId,
            district.Name,
            new SalespersonRecord(district.SalespersonId, district.SalespersonName),
            secondaries,
            stores);
    }

    public async Task AddSecondaryAsync(int districtId, int salespersonId, CancellationToken ct)
    {
        const string sql = """
            INSERT INTO dbo.DistrictSalesperson (DistrictId, SalespersonId, Role)
            VALUES (@DistrictId, @SalespersonId, N'Secondary');
            """;

        await using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { DistrictId = districtId, SalespersonId = salespersonId }, cancellationToken: ct));
    }

    public async Task RemoveSecondaryAsync(int districtId, int salespersonId, CancellationToken ct)
    {
        const string sql = """
            DELETE FROM dbo.DistrictSalesperson
            WHERE DistrictId = @DistrictId AND SalespersonId = @SalespersonId AND Role = N'Secondary';
            """;

        await using var connection = connectionFactory.Create();
        await connection.ExecuteAsync(new CommandDefinition(
            sql, new { DistrictId = districtId, SalespersonId = salespersonId }, cancellationToken: ct));
    }

    // The new primary's other role must go first, or the trigger rejects the update. Both or neither.
    public async Task ReplacePrimaryAsync(int districtId, int salespersonId, CancellationToken ct)
    {
        const string removeRole = """
            DELETE FROM dbo.DistrictSalesperson
            WHERE DistrictId = @DistrictId AND SalespersonId = @SalespersonId;
            """;
        const string setPrimary = """
            UPDATE dbo.District
            SET PrimarySalespersonId = @SalespersonId
            WHERE DistrictId = @DistrictId;
            """;
        var parameters = new { DistrictId = districtId, SalespersonId = salespersonId };

        await using var connection = connectionFactory.Create();
        await connection.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        await connection.ExecuteAsync(new CommandDefinition(removeRole, parameters, transaction, cancellationToken: ct));
        await connection.ExecuteAsync(new CommandDefinition(setPrimary, parameters, transaction, cancellationToken: ct));

        await transaction.CommitAsync(ct);
    }

    private sealed record DistrictRow(int DistrictId, string Name, int SalespersonId, string SalespersonName);
}
