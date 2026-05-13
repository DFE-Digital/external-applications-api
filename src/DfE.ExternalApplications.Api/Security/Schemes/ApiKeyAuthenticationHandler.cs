using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DfE.ExternalApplications.Api.Security.Schemes;

/// <summary>
/// Authenticates a request based on a shared <c>X-Api-Key</c> header. The key value is SHA-256
/// hashed and looked up in <see cref="ITenantAuthProviderRegistry"/> - we never store or compare
/// raw keys. On success the matched <see cref="TenantAuthProvider"/> is stashed in
/// <c>HttpContext.Items[AuthorizationExtensions.MatchedAuthProviderKey]</c> so the
/// <c>ServiceCallers</c> policy and <see cref="Security.TenantClaimsTransformation"/> can read it
/// without recomputing the lookup.
/// </summary>
public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<ApiKeyAuthenticationOptions> options,
    ILoggerFactory loggerFactory,
    UrlEncoder encoder,
    ITenantAuthProviderRegistry registry)
    : AuthenticationHandler<ApiKeyAuthenticationOptions>(options, loggerFactory, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var headerName = Options.HeaderName;
        if (!Request.Headers.TryGetValue(headerName, out var rawKeyValues))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var rawKey = rawKeyValues.ToString();
        if (string.IsNullOrWhiteSpace(rawKey))
        {
            return Task.FromResult(AuthenticateResult.Fail($"Header {headerName} is empty."));
        }

        var hash = HashKey(rawKey);
        var provider = registry.GetByApiKeyHash(hash);
        if (provider is null)
        {
            return Task.FromResult(AuthenticateResult.Fail("Unknown API key."));
        }

        Context.Items[AuthorizationExtensions.MatchedAuthProviderKey] = provider;

        var claims = new List<Claim>
        {
            new("tenant_id", provider.TenantId.ToString()),
            new("auth_provider", provider.Name),
            new("is_service", provider.IsServicePrincipal ? "true" : "false")
        };

        if (provider.Roles is not null)
        {
            claims.AddRange(provider.Roles.Select(r => new Claim(ClaimTypes.Role, r)));
        }

        var identity = new ClaimsIdentity(claims, AuthConstants.ApiKey);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    /// <summary>SHA-256 lower-case hex digest used for the registry lookup.</summary>
    public static string HashKey(string rawKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawKey));
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes)
        {
            sb.Append(b.ToString("x2"));
        }
        return sb.ToString();
    }
}
