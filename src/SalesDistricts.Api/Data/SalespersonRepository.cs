using Dapper;
using SalesDistricts.Api.Database;

namespace SalesDistricts.Api.Data;

public interface ISalespersonRepository
{
    Task<IReadOnlyList<SalespersonRecord>> GetAllAsync(CancellationToken ct);
    Task<bool> ExistsAsync(int salespersonId, CancellationToken ct);
}

public sealed class SalespersonRepository(IDbConnectionFactory connectionFactory) : ISalespersonRepository
{
    public async Task<IReadOnlyList<SalespersonRecord>> GetAllAsync(CancellationToken ct)
    {
        const string sql = "SELECT SalespersonId, Name FROM dbo.Salesperson ORDER BY Name;";

        await using var connection = connectionFactory.Create();
        var salespeople = await connection.QueryAsync<SalespersonRecord>(new CommandDefinition(sql, cancellationToken: ct));
        return salespeople.AsList();
    }

    public async Task<bool> ExistsAsync(int salespersonId, CancellationToken ct)
    {
        const string sql = "SELECT CAST(COUNT(1) AS bit) FROM dbo.Salesperson WHERE SalespersonId = @SalespersonId;";

        await using var connection = connectionFactory.Create();
        return await connection.ExecuteScalarAsync<bool>(new CommandDefinition(
            sql, new { SalespersonId = salespersonId }, cancellationToken: ct));
    }
}
