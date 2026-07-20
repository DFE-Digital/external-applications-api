using System.Security.Claims;
using GovUK.Dfe.FlexForms.Api.Security.Handlers;
using GovUK.Dfe.FlexForms.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace GovUK.Dfe.FlexForms.Api.Tests.Security.Handlers;

public class PlatformHostRoleAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenPlatformHostReadRolePresent()
    {
        var handler = new PlatformHostRoleAuthorizationHandler();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("roles", PlatformConstants.HostReadAppRole)
        ],
        authenticationType: "Bearer"));

        var context = new AuthorizationHandlerContext(
            [new PlatformHostRoleRequirement()],
            user,
            resource: null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task HandleAsync_ShouldNotSucceed_WhenRoleMissing()
    {
        var handler = new PlatformHostRoleAuthorizationHandler();
        var user = new ClaimsPrincipal(new ClaimsIdentity([], authenticationType: "Bearer"));

        var context = new AuthorizationHandlerContext(
            [new PlatformHostRoleRequirement()],
            user,
            resource: null);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}
