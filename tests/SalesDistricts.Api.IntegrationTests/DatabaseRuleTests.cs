using Dapper;
using Microsoft.Data.SqlClient;

namespace SalesDistricts.Api.IntegrationTests;

// Seed data: Anna (1) is primary of North Denmark (1); Bo (2) is a secondary there; Gustav (7) has no district.
// Every test runs inside a transaction that is rolled back, so the seed data never changes.
public sealed class DatabaseRuleTests(SqlServerFixture database) : IAsyncLifetime
{
    private const int NotNull = 515, ForeignKeyOrCheck = 547, Unique = 2627;
    private const int PrimaryHasRole = 50001, RoleHolderMadePrimary = 50002;

    private SqlConnection _connection = null!;
    private SqlTransaction _transaction = null!;

    public async ValueTask InitializeAsync()
    {
        _connection = new SqlConnection(database.ConnectionString);
        await _connection.OpenAsync();
        _transaction = _connection.BeginTransaction();
    }

    public async ValueTask DisposeAsync()
    {
        await _transaction.RollbackAsync();
        await _connection.DisposeAsync();
    }

    [Fact]
    public Task District_must_have_a_primary() =>
        AssertFails(NotNull, "INSERT INTO dbo.District (Name, PrimarySalespersonId) VALUES (N'New', NULL)");

    [Fact]
    public Task District_names_are_unique_ignoring_case() =>
        AssertFails(Unique, "INSERT INTO dbo.District (Name, PrimarySalespersonId) VALUES (N'north denmark', 2)");

    [Fact]
    public Task Store_must_belong_to_an_existing_district() =>
        AssertFails(ForeignKeyOrCheck, "INSERT INTO dbo.Store (Name, DistrictId) VALUES (N'Lost', 999)");

    [Fact]
    public async Task Salesperson_can_be_primary_of_several_districts()
    {
        await Execute("UPDATE dbo.District SET PrimarySalespersonId = 1 WHERE DistrictId = 4");

        var count = await _connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM dbo.District WHERE PrimarySalespersonId = 1", transaction: _transaction);
        Assert.Equal(3, count);
    }

    [Fact]
    public Task Primary_cannot_also_hold_a_role() =>
        AssertFails(PrimaryHasRole, "INSERT INTO dbo.DistrictSalesperson VALUES (1, 1, N'Secondary')");

    [Fact]
    public Task Role_holder_cannot_be_made_primary_directly() =>
        AssertFails(RoleHolderMadePrimary, "UPDATE dbo.District SET PrimarySalespersonId = 2 WHERE DistrictId = 1");

    [Fact]
    public Task Multi_row_insert_is_rejected_when_any_row_breaks_the_rule() =>
        AssertFails(PrimaryHasRole, "INSERT INTO dbo.DistrictSalesperson VALUES (3, 7, N'Secondary'), (3, 1, N'Secondary')");

    [Fact]
    public async Task Role_holder_can_be_promoted_after_removing_the_role()
    {
        await Execute("DELETE FROM dbo.DistrictSalesperson WHERE DistrictId = 1 AND SalespersonId = 2");
        await Execute("UPDATE dbo.District SET PrimarySalespersonId = 2 WHERE DistrictId = 1");

        var primary = await _connection.ExecuteScalarAsync<int>(
            "SELECT PrimarySalespersonId FROM dbo.District WHERE DistrictId = 1", transaction: _transaction);
        Assert.Equal(2, primary);
    }

    private Task Execute(string sql) => _connection.ExecuteAsync(sql, transaction: _transaction);

    // A trigger error dooms the transaction, so each test makes at most one failing statement.
    private async Task AssertFails(int expectedError, string sql)
    {
        var ex = await Assert.ThrowsAsync<SqlException>(() => Execute(sql));
        Assert.Equal(expectedError, ex.Number);
    }
}
