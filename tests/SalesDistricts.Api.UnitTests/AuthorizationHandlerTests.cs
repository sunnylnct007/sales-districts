using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using SalesDistricts.Api.Authorization;

namespace SalesDistricts.Api.UnitTests;

public sealed class AuthorizationHandlerTests
{
    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    private static ClaimsPrincipal SignedIn(params string[] roles) =>
        new(new ClaimsIdentity(roles.Select(r => new Claim(ClaimTypes.Role, r)), authenticationType: "Test"));

    [Fact]
    public async Task Viewing_requires_a_signed_in_user()
    {
        Assert.True(await Succeeds(new CanViewDistrictsHandler(), new CanViewDistrictsRequirement(), SignedIn()));
        Assert.False(await Succeeds(new CanViewDistrictsHandler(), new CanViewDistrictsRequirement(), Anonymous));
    }

    [Fact]
    public async Task Editing_requires_the_sales_manager_role()
    {
        Assert.True(await Succeeds(new CanEditAssignmentsHandler(), new CanEditAssignmentsRequirement(), SignedIn(Roles.SalesManager)));
        Assert.False(await Succeeds(new CanEditAssignmentsHandler(), new CanEditAssignmentsRequirement(), SignedIn("Viewer")));
    }

    [Fact]
    public async Task Allow_all_handler_lets_anonymous_users_do_everything()
    {
        var context = new AuthorizationHandlerContext(
            [new CanViewDistrictsRequirement(), new CanEditAssignmentsRequirement()], Anonymous, resource: null);

        await new AllowAllHandler().HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    private static async Task<bool> Succeeds(IAuthorizationHandler handler, IAuthorizationRequirement requirement, ClaimsPrincipal user)
    {
        var context = new AuthorizationHandlerContext([requirement], user, resource: null);
        await handler.HandleAsync(context);
        return context.HasSucceeded;
    }
}
