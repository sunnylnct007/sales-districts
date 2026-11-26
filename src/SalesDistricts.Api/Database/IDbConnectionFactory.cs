using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace SalesDistricts.Api.Database;

public interface IDbConnectionFactory
{
    DbConnection Create();
}

public sealed class SqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public DbConnection Create() => new SqlConnection(connectionString);
}
