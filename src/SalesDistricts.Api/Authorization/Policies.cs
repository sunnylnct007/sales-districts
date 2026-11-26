using Microsoft.AspNetCore.Authorization;

namespace SalesDistricts.Api.Authorization;

public static class Policies
{
    public const string CanViewDistricts = nameof(CanViewDistricts);
    public const string CanEditAssignments = nameof(CanEditAssignments);
}

public static class Roles
{
    public const string SalesManager = nameof(SalesManager);
}

public sealed class CanViewDistrictsRequirement : IAuthorizationRequirement;

public sealed class CanEditAssignmentsRequirement : IAuthorizationRequirement;
