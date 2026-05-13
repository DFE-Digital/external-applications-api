using System.Security.Claims;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Infrastructure.Security;
using Microsoft.AspNetCore.Http;

namespace DfE.ExternalApplications.Api.Security;

/// <summary>
/// Single source of truth for projecting a matched <see cref="TenantAuthProvider"/> into a
/// <see cref="ClaimsPrincipal"/>. Shared by every authentication scheme (<c>ApiKey</c>,
/// <c>Mtls</c>, and any future provider) so the contract for what claims a tenant principal
/// carries lives in exactly one place and is independently testable.
/// <para>
/// Stashes the matched provider in <see cref="HttpContext.Items"/> under
/// <see cref="AuthConstants.MatchedAuthProviderKey"/> so the <c>ServiceCallers</c> policy and
/// <see cref="TenantClaimsTransformation"/> can read it without recomputing the lookup.
/// </para>
/// </summary>
public static class TenantAuthPrincipalFactory
{
    /// <summary>
    /// Builds a <see cref="ClaimsPrincipal"/> identity for <paramref name="provider"/> on the
    /// <paramref name="schemeName"/> authentication scheme.
    /// </summary>
    /// <param name="provider">The matched tenant auth provider.</param>
    /// <param name="schemeName">The scheme used to authenticate this request.</param>
    /// <param name="additionalClaims">Optional claims from the underlying transport (e.g. cert subject).</param>
    public static ClaimsPrincipal BuildPrincipal(
        TenantAuthProvider provider,
        string schemeName,
        IEnumerable<Claim>? additionalClaims = null)
    {
        var claims = TenantAuthClaimsBuilder.Build(provider, additionalClaims);
        var identity = new ClaimsIdentity(claims, schemeName);
        return new ClaimsPrincipal(identity);
    }

    /// <summary>
    /// Stashes <paramref name="provider"/> in <see cref="HttpContext.Items"/> for downstream
    /// authorization handlers / claims transformations.
    /// </summary>
    public static void StashProvider(HttpContext httpContext, TenantAuthProvider provider)
    {
        httpContext.Items[AuthConstants.MatchedAuthProviderKey] = provider;
    }
}
