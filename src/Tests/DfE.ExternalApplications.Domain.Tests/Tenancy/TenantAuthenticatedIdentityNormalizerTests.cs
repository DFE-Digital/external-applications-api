using System.Security.Claims;
using DfE.ExternalApplications.Domain.Tenancy;
using Xunit;

namespace DfE.ExternalApplications.Domain.Tests.Tenancy;

public class TenantAuthenticatedIdentityNormalizerTests
{
    [Fact]
    public void Apply_WithMatchedProvider_FillsTenantIdAndIsServiceAndRoles()
    {
        var tenantId = Guid.NewGuid();
        var provider = new TenantAuthProvider(
            TenantId: tenantId,
            Name: "api",
            Kind: TenantAuthProviderKind.ApiKey,
            IsServicePrincipal: true,
            Roles: new[] { "Caller" });

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, "x")], authenticationType: "Test");
        TenantAuthenticatedIdentityNormalizer.Apply(identity, provider);

        Assert.Equal(tenantId.ToString(), identity.FindFirst(TenantAuthClaimTypes.TenantId)?.Value);
        Assert.Equal("true", identity.FindFirst(TenantAuthClaimTypes.IsService)?.Value);
        Assert.True(identity.HasClaim(ClaimTypes.Role, "Caller"));
    }

    [Fact]
    public void Apply_AddsEmailFromAlternateClaims()
    {
        var identity = new ClaimsIdentity(
            [new Claim(TenantAuthClaimTypes.PreferredUsername, "u@school.uk")],
            authenticationType: "Test");
        TenantAuthenticatedIdentityNormalizer.Apply(identity, matchedProvider: null);

        Assert.Equal("u@school.uk", identity.FindFirst(ClaimTypes.Email)?.Value);
    }

    [Fact]
    public void Apply_DoesNotOverwriteExistingTenantIdClaim()
    {
        var existing = Guid.NewGuid();
        var provider = new TenantAuthProvider(
            TenantId: Guid.NewGuid(),
            Name: "p",
            Kind: TenantAuthProviderKind.JwtHmac,
            IsServicePrincipal: false);

        var identity = new ClaimsIdentity(
            [new Claim(TenantAuthClaimTypes.TenantId, existing.ToString())],
            authenticationType: "Test");
        TenantAuthenticatedIdentityNormalizer.Apply(identity, provider);

        Assert.Equal(existing.ToString(), identity.FindFirst(TenantAuthClaimTypes.TenantId)?.Value);
    }
}
