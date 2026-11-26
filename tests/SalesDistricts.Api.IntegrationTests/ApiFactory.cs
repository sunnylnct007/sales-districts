using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace SalesDistricts.Api.IntegrationTests;

// One running API per test class, pointed at the shared SQL Server container.
public class ApiFactory(SqlServerFixture database) : WebApplicationFactory<Program>
{
    protected virtual bool AuthenticationEnabled => false;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseSetting("ConnectionStrings:Districts", database.ConnectionString);
        builder.UseSetting("Authentication:Enabled", AuthenticationEnabled.ToString());
        builder.UseSetting("Authentication:Authority", "https://login.microsoftonline.com/test/v2.0");
        builder.UseSetting("Authentication:Audience", "api://sales-districts");
    }
}

public sealed class AuthenticatedApiFactory(SqlServerFixture database) : ApiFactory(database)
{
    protected override bool AuthenticationEnabled => true;
}
