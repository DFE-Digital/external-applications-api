using System.Security.Claims;
using DfE.ExternalApplications.Api.Security.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace DfE.ExternalApplications.Api.Tests.Security.Handlers;

public class ApplicationFilesPermissionHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSucceed_WhenClaimMatchesRoute()
    {
        // Arrange
        var requirement = new ApplicationFilesPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["applicationId"] = "123";
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var claims = new[] { new Claim("permission", "ApplicationFiles:123:Read") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new ApplicationFilesPermissionHandler(accessor);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenRouteMissing()
    {
        // Arrange
        var requirement = new ApplicationFilesPermissionRequirement("Read");
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext());
        var claims = new[] { new Claim("permission", "ApplicationFiles:123:Read") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new ApplicationFilesPermissionHandler(accessor);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenClaimMissing()
    {
        // Arrange
        var requirement = new ApplicationFilesPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["applicationId"] = "123";
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new ApplicationFilesPermissionHandler(accessor);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenClaimActionDoesNotMatch()
    {
        // Arrange
        var requirement = new ApplicationFilesPermissionRequirement("Write");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["applicationId"] = "123";
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var claims = new[] { new Claim("permission", "ApplicationFiles:123:Read") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new ApplicationFilesPermissionHandler(accessor);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenUserIsAdmin()
    {
        // Arrange
        var requirement = new ApplicationFilesPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var claims = new[] { new Claim(ClaimTypes.Role, "Admin") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "Test"));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new ApplicationFilesPermissionHandler(accessor);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenClaimApplicationIdDoesNotMatch()
    {
        // Arrange
        var requirement = new ApplicationFilesPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["applicationId"] = "456";
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var claims = new[] { new Claim("permission", "ApplicationFiles:123:Read") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new ApplicationFilesPermissionHandler(accessor);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }
}
