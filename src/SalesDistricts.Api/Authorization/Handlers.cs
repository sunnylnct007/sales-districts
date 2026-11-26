using Microsoft.AspNetCore.Authorization;

namespace SalesDistricts.Api.Authorization;

public sealed class CanViewDistrictsHandler : AuthorizationHandler<CanViewDistrictsRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, CanViewDistrictsRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated == true)
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

public sealed class CanEditAssignmentsHandler : AuthorizationHandler<CanEditAssignmentsRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context, CanEditAssignmentsRequirement requirement)
    {
        if (context.User.IsInRole(Roles.SalesManager))
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}

// Used only when authentication is disabled (Development).
public sealed class AllowAllHandler : IAuthorizationHandler
{
    public Task HandleAsync(AuthorizationHandlerContext context)
    {
        foreach (var requirement in context.PendingRequirements.ToList())
            context.Succeed(requirement);

        return Task.CompletedTask;
    }
}
