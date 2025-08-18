using System.Security.Claims;
using DfE.ExternalApplications.Api.Security.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace DfE.ExternalApplications.Api.Tests.Security.Handlers;

public class NotificationsPermissionHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSucceed_WithNotificationPermissionForCurrentUser()
    {
        // Arrange
        var requirement = new NotificationsPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var userEmail = "user@example.com";
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, userEmail),
            new Claim("permission", $"Notifications:{userEmail}:Read")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new NotificationsPermissionHandler(accessor);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }


    [Fact]
    public async Task Handle_ShouldSucceed_WithAppIdClaim()
    {
        // Arrange
        var requirement = new NotificationsPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var appId = "test-app-id";
        var claims = new[]
        {
            new Claim("appid", appId),
            new Claim("permission", $"Notifications:{appId}:Read")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new NotificationsPermissionHandler(accessor);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WithAzpClaim()
    {
        // Arrange
        var requirement = new NotificationsPermissionRequirement("Write");
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var azp = "test-azp-id";
        var claims = new[]
        {
            new Claim("azp", azp),
            new Claim("permission", $"Notifications:{azp}:Write")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new NotificationsPermissionHandler(accessor);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldNotSucceed_WithoutValidUserClaim()
    {
        // Arrange
        var requirement = new NotificationsPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var claims = new[]
        {
            new Claim("permission", "Notifications:other@example.com:Read")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new NotificationsPermissionHandler(accessor);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldNotSucceed_WithWrongPermissionAction()
    {
        // Arrange
        var requirement = new NotificationsPermissionRequirement("Write");
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var userEmail = "user@example.com";
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, userEmail),
            new Claim("permission", $"Notifications:{userEmail}:Read") // Has Read but needs Write
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new NotificationsPermissionHandler(accessor);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldNotSucceed_WithoutAnyUserIdentifier()
    {
        // Arrange
        var requirement = new NotificationsPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var claims = new[]
        {
            new Claim("permission", "Notifications:user@example.com:Read")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new NotificationsPermissionHandler(accessor);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldNotSucceed_WithApplicationIdButNoApplicationPermission()
    {
        // Arrange
        var requirement = new NotificationsPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        var applicationId = Guid.NewGuid().ToString();
        httpContext.Request.RouteValues["applicationId"] = applicationId;
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var userEmail = "user@example.com";
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, userEmail),
            new Claim("permission", $"Notifications:{userEmail}:Read")
            // Missing ApplicationFiles permission for the specific applicationId
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new NotificationsPermissionHandler(accessor);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded); // Should succeed with user permission as fallback
    }

    [Theory]
    [InlineData("Read")]
    [InlineData("Write")]
    [InlineData("Delete")]
    public async Task Handle_ShouldSucceed_WithVariousActionTypes(string action)
    {
        // Arrange
        var requirement = new NotificationsPermissionRequirement(action);
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var userEmail = "user@example.com";
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, userEmail),
            new Claim("permission", $"Notifications:{userEmail}:{action}")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new NotificationsPermissionHandler(accessor);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }
}
