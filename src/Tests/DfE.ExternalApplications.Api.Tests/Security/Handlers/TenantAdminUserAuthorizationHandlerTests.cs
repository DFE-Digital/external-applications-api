using System.Security.Claims;
using DfE.ExternalApplications.Api.Security.Handlers;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using NSubstitute;

namespace DfE.ExternalApplications.Api.Tests.Security.Handlers;

public class TenantAdminUserAuthorizationHandlerTests
{
    private readonly IHttpContextAccessor _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
    private readonly TenantAdminUserAuthorizationHandler _handler;
    private readonly TenantAdminUserRequirement _requirement = new();

    public TenantAdminUserAuthorizationHandlerTests()
    {
        _handler = new TenantAdminUserAuthorizationHandler(_httpContextAccessor);
        _httpContextAccessor.HttpContext.Returns(new DefaultHttpContext());
    }

    [Fact]
    public async Task Handle_ShouldSucceed_ForInteractiveAdminUserJwt()
    {
        var user = CreatePrincipal(
            roles: [RoleNames.Admin],
            email: "admin@example.com",
            isService: false);

        var context = new AuthorizationHandlerContext([_requirement], user, null);
        await _handler.HandleAsync(context);

        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldFail_ForClientCredentialsWithAdminRole()
    {
        var user = CreatePrincipal(
            roles: [RoleNames.Admin, "ServiceCaller"],
            email: null,
            isService: true);

        var context = new AuthorizationHandlerContext([_requirement], user, null);
        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldFail_ForAdminRoleWithoutEmail()
    {
        var user = CreatePrincipal(
            roles: [RoleNames.Admin],
            email: null,
            isService: false);

        var context = new AuthorizationHandlerContext([_requirement], user, null);
        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenMatchedProviderIsServicePrincipal()
    {
        var http = new DefaultHttpContext();
        http.Items[AuthConstants.MatchedAuthProviderKey] = new TenantAuthProvider(
            TenantId: Guid.NewGuid(),
            Name: "azure-ad-svc",
            Kind: TenantAuthProviderKind.EntraOidc,
            IsServicePrincipal: true,
            Roles: [RoleNames.Admin]);
        _httpContextAccessor.HttpContext.Returns(http);

        var user = CreatePrincipal(
            roles: [RoleNames.Admin],
            email: "sp@apps.local",
            isService: false); // claim missing; provider stash still rejects

        var context = new AuthorizationHandlerContext([_requirement], user, null);
        await _handler.HandleAsync(context);

        Assert.False(context.HasSucceeded);
    }

    private static ClaimsPrincipal CreatePrincipal(string[] roles, string? email, bool isService)
    {
        var claims = new List<Claim>();
        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        if (!string.IsNullOrEmpty(email))
        {
            claims.Add(new Claim(ClaimTypes.Email, email));
        }

        if (isService)
        {
            claims.Add(new Claim(TenantAuthClaimTypes.IsService, "true"));
        }

        return new ClaimsPrincipal(new ClaimsIdentity(claims, authenticationType: "Test"));
    }
}
