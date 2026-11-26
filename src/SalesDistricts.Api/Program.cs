using SalesDistricts.Api.Authorization;
using SalesDistricts.Api.Data;
using SalesDistricts.Api.Database;
using SalesDistricts.Api.Endpoints;
using SalesDistricts.Api.Errors;
using SalesDistricts.Api.Observability;
using SalesDistricts.Api.Services;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("Districts")
    ?? throw new InvalidOperationException("Connection string 'Districts' is missing.");

builder.AddObservability();
builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddExceptionHandler<DatabaseConflictExceptionHandler>();
builder.Services.AddDistrictAuthorization(builder.Configuration, builder.Environment);
builder.Services.AddSingleton<IDbConnectionFactory>(new SqlConnectionFactory(connectionString));
builder.Services.AddScoped<IDistrictRepository, DistrictRepository>();
builder.Services.AddScoped<IDistrictService, DistrictService>();
builder.Services.AddScoped<ISalespersonRepository, SalespersonRepository>();
builder.Services.AddScoped<ISalespersonService, SalespersonService>();

var app = builder.Build();

DatabaseMigrator.Migrate(connectionString);

if (app.Configuration.GetValue("OpenApi:Enabled", defaultValue: true))
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseExceptionHandler();
app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthEndpoints();
app.MapDistrictEndpoints();
app.MapSalespersonEndpoints();

app.Run();
