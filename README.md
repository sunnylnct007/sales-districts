# Sales Districts

Manage which salespeople cover which districts. A district has one primary salesperson and any number of secondary salespeople, and contains stores.

| Folder | Contents |
|---|---|
| `database/` | SQL Server schema and test data |
| `src/SalesDistricts.Api` | ASP.NET Core Web API |
| `src/SalesDistricts.Wpf` | WPF desktop client |
| `src/SalesDistricts.Client` | ViewModels and API client used by the WPF app |
| `tests/` | Unit and integration tests |
| `docs/` | Data model diagram |

## Prerequisites

- Windows
- .NET 10 SDK
- SQL Server Express (`.\SQLEXPRESS`), or change the connection string in `src/SalesDistricts.Api/appsettings.Development.json`
- Docker, only for the integration tests

## Run

Open `SalesDistricts.slnx` and run **API + WPF**. The database is created with test data on first start.

Or from the command line:

```bash
dotnet run --project src/SalesDistricts.Api --launch-profile https
dotnet run --project src/SalesDistricts.Wpf
```

API documentation: `https://localhost:7217/scalar`

## Test

```bash
dotnet test
```
