using Dapper;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SalesDistricts.Api.Database;

namespace SalesDistricts.Api.Observability;

public sealed class DatabaseHealthCheck(IDbConnectionFactory connectionFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken ct = default)
    {
        try
        {
            await using var connection = connectionFactory.Create();
            await connection.ExecuteScalarAsync<int>(new CommandDefinition("SELECT 1;", cancellationToken: ct));
            return HealthCheckResult.Healthy();
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("The database is not reachable.", ex);
        }
    }
}
