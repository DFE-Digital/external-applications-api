using System.Security.Claims;
using DfE.ExternalApplications.Api.Security.Handlers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;

namespace DfE.ExternalApplications.Api.Tests.Security.Handlers;

public class ApplicationPermissionHandlerTests
{
    [Fact]
    public async Task Handle_ShouldSucceed_WhenClaimMatchesRoute()
    {
        var requirement = new ApplicationPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["applicationId"] = "123";
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var claims = new[] { new Claim("permission", "Application:123:Read") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new ApplicationPermissionHandler(accessor);

        await handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenRouteMissing()
    {
        var requirement = new ApplicationPermissionRequirement("Read");
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext());
        var claims = new[] { new Claim("permission", "Application:123:Read") };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims));
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new ApplicationPermissionHandler(accessor);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenClaimMissing()
    {
        var requirement = new ApplicationPermissionRequirement("Read");
        var httpContext = new DefaultHttpContext();
        httpContext.Request.RouteValues["applicationId"] = "123";
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(httpContext);
        var user = new ClaimsPrincipal(new ClaimsIdentity());
        var context = new AuthorizationHandlerContext([requirement], user, null);
        var handler = new ApplicationPermissionHandler(accessor);

        await handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }
}