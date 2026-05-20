using System.Security.Claims;
using DfE.ExternalApplications.Api.Security.Handlers;
using DfE.ExternalApplications.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace DfE.ExternalApplications.Api.Tests.Security.Handlers;

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
