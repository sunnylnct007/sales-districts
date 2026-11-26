using Microsoft.Data.SqlClient;
using SalesDistricts.Api.Database;
using SalesDistricts.Api.IntegrationTests;
using Testcontainers.MsSql;
using Xunit.Sdk;
using Xunit.v3;

[assembly: AssemblyFixture(typeof(SqlServerFixture))]
// Test classes share one database, so run them one at a time: no blocking or deadlocks between them.
[assembly: Parallelization(Mode = ParallelMode.None)]

namespace SalesDistricts.Api.IntegrationTests;

// One SQL Server container for the whole test run, migrated with the real scripts.
public sealed class SqlServerFixture : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public string ConnectionString { get; private set; } = "";

    public async ValueTask InitializeAsync()
    {
        await _container.StartAsync();
        ConnectionString = new SqlConnectionStringBuilder(_container.GetConnectionString())
        {
            InitialCatalog = "SalesDistricts"
        }.ConnectionString;

        DatabaseMigrator.Migrate(ConnectionString);
    }

    public async ValueTask DisposeAsync() => await _container.DisposeAsync();
}
