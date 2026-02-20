using System.Security.Claims;
using DfE.ExternalApplications.Api.Security.Handlers;
using Microsoft.AspNetCore.Authorization;
using Xunit;

namespace DfE.ExternalApplications.Api.Tests.Security.Handlers;

public class AnyTemplatePermissionHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSucceed_WhenUserHasMatchingTemplateClaim()
    {
        // Arrange
        var requirement = new AnyTemplatePermissionRequirement("Read");
        var claims = new[] { new Claim("permission", "Template:some-template-id:Read") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new AnyTemplatePermissionHandler();

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenNoTemplateClaims()
    {
        // Arrange
        var requirement = new AnyTemplatePermissionRequirement("Read");
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new AnyTemplatePermissionHandler();

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenActionDoesNotMatch()
    {
        // Arrange
        var requirement = new AnyTemplatePermissionRequirement("Write");
        var claims = new[] { new Claim("permission", "Template:some-template-id:Read") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new AnyTemplatePermissionHandler();

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenClaimIsNotTemplateType()
    {
        // Arrange
        var requirement = new AnyTemplatePermissionRequirement("Read");
        var claims = new[] { new Claim("permission", "Application:some-id:Read") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new AnyTemplatePermissionHandler();

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenUserIsAdmin()
    {
        // Arrange
        var requirement = new AnyTemplatePermissionRequirement("Read");
        var claims = new[] { new Claim(ClaimTypes.Role, "Admin") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new AnyTemplatePermissionHandler();

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenMultipleTemplateClaimsExist_OneMatches()
    {
        // Arrange
        var requirement = new AnyTemplatePermissionRequirement("Read");
        var claims = new[]
        {
            new Claim("permission", "Template:template1:Write"),
            new Claim("permission", "Template:template2:Read")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new AnyTemplatePermissionHandler();

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }
}
