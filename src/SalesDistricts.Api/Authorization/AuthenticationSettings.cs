namespace SalesDistricts.Api.Authorization;

public sealed class AuthenticationSettings
{
    public const string SectionName = "Authentication";

    public bool Enabled { get; init; } = true;
    public string? Authority { get; init; }
    public string? Audience { get; init; }
}
