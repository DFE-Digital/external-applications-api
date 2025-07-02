using System.Security.Claims;
using DfE.ExternalApplications.Api.Security.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace DfE.ExternalApplications.Api.Tests.Security.Handlers;

public class UserPermissionHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSucceed_WithEmailRouteValue()
    {
        var requirement = new UserPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["email"] = "user@example.com";
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var claims = new[] { new Claim("permission", "User:user@example.com:Read") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new UserPermissionHandler(accessor);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WithEmailClaim()
    {
        var requirement = new UserPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var claims = new[]
        {
            new Claim(ClaimTypes.Email, "user@example.com"),
            new Claim("permission", "User:user@example.com:Read")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new UserPermissionHandler(accessor);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WithAppIdClaim()
    {
        var requirement = new UserPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var claims = new[]
        {
            new Claim("appid", "client1"),
            new Claim("permission", "User:client1:Read")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new UserPermissionHandler(accessor);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenNoIdentifier()
    {
        var requirement = new UserPermissionRequirement("Read");
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext());
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new UserPermissionHandler(accessor);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenClaimMissing()
    {
        var requirement = new UserPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["email"] = "user@example.com";
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new UserPermissionHandler(accessor);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}