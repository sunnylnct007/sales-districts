using DbUp;

namespace SalesDistricts.Api.Database;

public static class DatabaseMigrator
{
    public static void Migrate(string connectionString)
    {
        EnsureDatabase.For.SqlDatabase(connectionString);

        var result = DeployChanges.To
            .SqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(DatabaseMigrator).Assembly)
            .WithTransactionPerScript()
            .LogToNowhere()
            .Build()
            .PerformUpgrade();

        if (!result.Successful)
            throw new InvalidOperationException("Database migration failed.", result.Error);
    }
}
