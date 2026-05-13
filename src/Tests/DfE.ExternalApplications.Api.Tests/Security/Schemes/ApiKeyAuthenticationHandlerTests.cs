using System.Security.Claims;
using System.Text.Encodings.Web;
using DfE.ExternalApplications.Api.Security.Schemes;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace DfE.ExternalApplications.Api.Tests.Security.Schemes;

public class ApiKeyAuthenticationHandlerTests
{
    private static async Task<(AuthenticateResult Result, HttpContext Context)> RunHandlerAsync(
        ITenantAuthProviderRegistry registry,
        Action<HttpContext> setupRequest)
    {
        var options = Substitute.For<IOptionsMonitor<ApiKeyAuthenticationOptions>>();
        options.Get(Arg.Any<string>()).Returns(new ApiKeyAuthenticationOptions());
        options.CurrentValue.Returns(new ApiKeyAuthenticationOptions());

        var loggerFactory = LoggerFactory.Create(_ => { });
        var encoder = UrlEncoder.Default;

        var handler = new ApiKeyAuthenticationHandler(options, loggerFactory, encoder, registry);

        var context = new DefaultHttpContext();
        setupRequest(context);
        var scheme = new AuthenticationScheme(AuthConstants.ApiKey, null, typeof(ApiKeyAuthenticationHandler));
        await handler.InitializeAsync(scheme, context);

        var result = await handler.AuthenticateAsync();
        return (result, context);
    }

    [Fact]
    public async Task NoHeader_ReturnsNoResult()
    {
        var registry = Substitute.For<ITenantAuthProviderRegistry>();

        var (result, _) = await RunHandlerAsync(registry, _ => { });

        Assert.True(result.None);
        Assert.False(result.Succeeded);
    }

    [Fact]
    public async Task UnknownKey_Fails()
    {
        var registry = Substitute.For<ITenantAuthProviderRegistry>();
        registry.GetByApiKeyHash(Arg.Any<string>()).Returns((TenantAuthProvider?)null);

        var (result, _) = await RunHandlerAsync(registry, ctx =>
            ctx.Request.Headers["X-Api-Key"] = "some-raw-key");

        Assert.False(result.Succeeded);
        Assert.NotNull(result.Failure);
        Assert.Contains("Unknown API key", result.Failure!.Message);
    }

    [Fact]
    public async Task KnownKey_BuildsPrincipalWithTenantClaimsAndStashesProvider()
    {
        var tenantId = Guid.NewGuid();
        var provider = new TenantAuthProvider(
            TenantId: tenantId,
            Name: "tenantA-backend",
            Kind: TenantAuthProviderKind.ApiKey,
            IsServicePrincipal: true,
            ApiKeyHash: TenantApiKeyHasher.Hash("raw"),
            Roles: new[] { "ServiceCaller" });

        var registry = Substitute.For<ITenantAuthProviderRegistry>();
        registry.GetByApiKeyHash(TenantApiKeyHasher.Hash("raw")).Returns(provider);

        var (result, ctx) = await RunHandlerAsync(registry, c => c.Request.Headers["X-Api-Key"] = "raw");

        Assert.True(result.Succeeded);
        Assert.Equal(tenantId.ToString(), result.Principal!.FindFirst(TenantAuthClaimTypes.TenantId)?.Value);
        Assert.Equal("true", result.Principal!.FindFirst(TenantAuthClaimTypes.IsService)?.Value);
        Assert.Equal("tenantA-backend", result.Principal!.FindFirst(TenantAuthClaimTypes.AuthProvider)?.Value);
        Assert.True(result.Principal!.HasClaim(ClaimTypes.Role, "ServiceCaller"));
        Assert.Same(provider, ctx.Items[AuthConstants.MatchedAuthProviderKey]);
    }
}
