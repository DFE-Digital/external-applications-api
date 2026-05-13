using System.Security.Claims;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Infrastructure.Security;
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
        var matched = http?.Items[AuthConstants.MatchedAuthProviderKey] as TenantAuthProvider;

        TenantAuthenticatedIdentityNormalizer.Apply(identity, matched);

        return Task.FromResult(principal);
    }
}
