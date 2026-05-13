using System.Security.Claims;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace DfE.ExternalApplications.Api.Security;

/// <summary>
/// Normalizes the authenticated principal across all schemes (<c>TenantBearer</c>, <c>ApiKey</c>,
/// <c>Mtls</c>) so authorization handlers and downstream business logic do not have to branch on
/// the active scheme or rummage through scheme-specific claim names.
/// <para>
/// Guarantees on every authenticated request:
/// </para>
/// <list type="bullet">
///   <item><description><c>tenant_id</c> claim is present (matching the resolved tenant).</description></item>
///   <item><description><c>is_service</c> claim is present (<c>true</c>/<c>false</c>).</description></item>
///   <item><description><c>email</c> claim is present when the underlying token/identity carried one.</description></item>
///   <item><description><c>roles</c> from the matched <see cref="TenantAuthProvider"/> are projected onto the principal.</description></item>
/// </list>
/// </summary>
public sealed class TenantClaimsTransformation(IHttpContextAccessor httpContextAccessor) : IClaimsTransformation
{
    public Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity is not ClaimsIdentity identity || !identity.IsAuthenticated)
        {
            return Task.FromResult(principal);
        }

        var http = httpContextAccessor.HttpContext;
        var matched = http?.Items[AuthorizationExtensions.MatchedAuthProviderKey] as TenantAuthProvider;

        AddIfMissing(identity, "tenant_id",
            matched?.TenantId.ToString()
            ?? identity.FindFirst("tenant_id")?.Value
            ?? identity.FindFirst("tid")?.Value);

        AddIfMissing(identity, "is_service",
            matched is not null
                ? (matched.IsServicePrincipal ? "true" : "false")
                : (identity.HasClaim(c => c.Type == "is_service")
                    ? identity.FindFirst("is_service")!.Value
                    : null));

        // Project provider-declared roles when not already present.
        if (matched?.Roles is { Count: > 0 })
        {
            foreach (var role in matched.Roles)
            {
                if (!identity.HasClaim(ClaimTypes.Role, role))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }
        }

        // Some IdPs use "emails" instead of ClaimTypes.Email; normalize.
        if (!identity.HasClaim(c => c.Type == ClaimTypes.Email))
        {
            var fallbackEmail = identity.FindFirst("email")?.Value
                ?? identity.FindFirst("preferred_username")?.Value;
            if (!string.IsNullOrEmpty(fallbackEmail))
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, fallbackEmail));
            }
        }

        return Task.FromResult(principal);
    }

    private static void AddIfMissing(ClaimsIdentity identity, string claimType, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (identity.HasClaim(c => c.Type == claimType)) return;
        identity.AddClaim(new Claim(claimType, value));
    }
}
