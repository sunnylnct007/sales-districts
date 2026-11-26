using System.ComponentModel.DataAnnotations;

namespace SalesDistricts.Api.Contracts;

public sealed record ReplacePrimaryRequest([property: Range(1, int.MaxValue)] int SalespersonId);
