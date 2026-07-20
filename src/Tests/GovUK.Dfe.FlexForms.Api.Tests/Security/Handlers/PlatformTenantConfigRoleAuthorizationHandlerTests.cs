using System.Security.Claims;
using GovUK.Dfe.FlexForms.Api.Security.Handlers;
using GovUK.Dfe.FlexForms.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace GovUK.Dfe.FlexForms.Api.Tests.Security.Handlers;

public class PlatformTenantConfigRoleAuthorizationHandlerTests
{
    [Fact]
    public async Task HandleAsync_ShouldSucceed_WhenTenantConfigReadRolePresent()
    {
        var handler = new PlatformTenantConfigRoleAuthorizationHandler();
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim("roles", PlatformConstants.TenantConfigReadAppRole)
        ],
        authenticationType: "Bearer"));

        var context = new AuthorizationHandlerContext(
            [new PlatformTenantConfigRoleRequirement()],
            user,
            resource: null);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }
}
