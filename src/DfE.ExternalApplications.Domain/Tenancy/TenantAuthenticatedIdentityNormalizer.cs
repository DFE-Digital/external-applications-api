using System.Security.Claims;

namespace DfE.ExternalApplications.Domain.Tenancy;

/// <summary>
/// Pure domain rules for normalizing an already-authenticated <see cref="ClaimsIdentity"/> after
/// the transport (bearer token, API key, mTLS) has run. Ensures a consistent claim shape for
/// authorization policies and application use cases without embedding those rules in ASP.NET
/// middleware types.
/// </summary>
public static class TenantAuthenticatedIdentityNormalizer
{
    /// <summary>
    /// Adds or supplements claims so the identity exposes <see cref="TenantAuthClaimTypes.TenantId"/>,
    /// <see cref="TenantAuthClaimTypes.IsService"/>, role claims from <paramref name="matchedProvider"/>,
    /// and a normalised <see cref="ClaimTypes.Email"/> when a suitable alternate claim exists.
    /// </summary>
    /// <param name="identity">The authenticated identity to mutate in-place.</param>
    /// <param name="matchedProvider">
    /// The provider stashed for the request after authentication, or <c>null</c> when only token
    /// claims are available (for example before stashing runs in tests).
    /// </param>
    public static void Apply(ClaimsIdentity identity, TenantAuthProvider? matchedProvider)
    {
        AddIfMissing(identity, TenantAuthClaimTypes.TenantId,
            matchedProvider?.TenantId.ToString()
            ?? identity.FindFirst(TenantAuthClaimTypes.TenantId)?.Value
            ?? identity.FindFirst(TenantAuthClaimTypes.AzureDirectoryTenantId)?.Value);

        AddIfMissing(identity, TenantAuthClaimTypes.IsService,
            matchedProvider is not null
                ? (matchedProvider.IsServicePrincipal ? "true" : "false")
                : (identity.HasClaim(c => c.Type == TenantAuthClaimTypes.IsService)
                    ? identity.FindFirst(TenantAuthClaimTypes.IsService)!.Value
                    : null));

        if (matchedProvider?.Roles is { Count: > 0 })
        {
            foreach (var role in matchedProvider.Roles)
            {
                if (!identity.HasClaim(ClaimTypes.Role, role))
                {
                    identity.AddClaim(new Claim(ClaimTypes.Role, role));
                }
            }
        }

        if (!identity.HasClaim(c => c.Type == ClaimTypes.Email))
        {
            var fallbackEmail = identity.FindFirst(TenantAuthClaimTypes.Email)?.Value
                ?? identity.FindFirst(TenantAuthClaimTypes.PreferredUsername)?.Value;
            if (!string.IsNullOrEmpty(fallbackEmail))
            {
                identity.AddClaim(new Claim(ClaimTypes.Email, fallbackEmail));
            }
        }
    }

    private static void AddIfMissing(ClaimsIdentity identity, string claimType, string? value)
    {
        if (string.IsNullOrEmpty(value)) return;
        if (identity.HasClaim(c => c.Type == claimType)) return;
        identity.AddClaim(new Claim(claimType, value));
    }
}
