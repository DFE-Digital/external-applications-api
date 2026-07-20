namespace GovUK.Dfe.FlexForms.Domain.Tenancy;

/// <summary>
/// Hot-reloadable, in-memory registry of <see cref="TenantAuthProvider"/> rows projected from
/// the tenant configuration database. Indexed by <c>Issuer</c> (possibly many rows per issuer
/// when several SaaS tenants share one Entra directory), <c>ApiKeyHash</c> and
/// <c>CertificateThumbprint</c> so each authentication scheme can resolve the matching provider.
/// <para>
/// The registry MUST subscribe to <see cref="ITenantConfigurationChangedNotifier.Changed"/> and
/// rebuild its indexes when notified, so adding a new tenant or rotating a signing key is a
/// pure DB write picked up within the configured refresh interval - no service restart.
/// </para>
/// <para>
/// This interface is intentionally framework-agnostic: it returns pure data records and does
/// not reference any token / JWT library. Resolving signing keys (an Infrastructure concern)
/// is split into a separate <c>ITenantSigningKeyResolver</c> living in the Infrastructure
/// layer so the Domain stays free of <c>Microsoft.IdentityModel.*</c> dependencies.
/// </para>
/// </summary>
public interface ITenantAuthProviderRegistry
{
    /// <summary>
    /// Find an auth provider that issues tokens with the given <paramref name="issuer"/> claim.
    /// </summary>
    /// <remarks>
    /// When several tenants share the same <c>iss</c> (e.g. one Entra directory), multiple
    /// providers may exist; this returns an arbitrary first match. Prefer
    /// <see cref="ResolveJwtBearerProvider"/> for bearer validation and stashing.
    /// </remarks>
    /// <param name="issuer">Value of the <c>iss</c> claim.</param>
    /// <returns>A matching provider, or <c>null</c> if no tenant claims this issuer.</returns>
    TenantAuthProvider? GetByIssuer(string issuer);

    /// <summary>
    /// Returns every registered provider whose <see cref="TenantAuthProvider.Issuer"/> equals
    /// <paramref name="issuer"/> (ordinal case-insensitive).
    /// </summary>
    IReadOnlyList<TenantAuthProvider> GetProvidersByIssuer(string issuer);

    /// <summary>
    /// Returns <c>true</c> when at least one provider is registered for <paramref name="issuer"/>.
    /// Used by JWT issuer validation when <c>iss</c> is shared across many app registrations.
    /// </summary>
    bool HasAnyProviderForIssuer(string issuer);

    /// <summary>
    /// Picks the single <see cref="TenantAuthProvider"/> for this bearer token within the resolved
    /// SaaS tenant using <c>iss</c>, token <c>aud</c> values, and optional Entra
    /// <c>azp</c>/<c>appid</c> (calling app registration id).
    /// </summary>
    /// <param name="issuer">Token <c>iss</c> claim.</param>
    /// <param name="tokenAudiences">Token <c>aud</c> value(s).</param>
    /// <param name="resolvedTenantId">Tenant from <c>X-Tenant-ID</c> / host resolution.</param>
    /// <param name="azpOrAppId">Entra <c>azp</c> or <c>appid</c> when present; disambiguates service principals.</param>
    /// <returns>The matching row, or <c>null</c> if none or ambiguous.</returns>
    TenantAuthProvider? ResolveJwtBearerProvider(
        string issuer,
        IEnumerable<string> tokenAudiences,
        Guid resolvedTenantId,
        string? azpOrAppId);

    /// <summary>
    /// Returns the first provider suitable for fetching signing keys for <paramref name="issuer"/>
    /// (OIDC discovery or symmetric HMAC). Used when many tenants share the same OIDC metadata.
    /// </summary>
    TenantAuthProvider? GetFirstSigningProviderForIssuer(string issuer);

    /// <summary>
    /// Validate that at least one of <paramref name="audiences"/> matches the audience(s)
    /// configured for the provider with the given <paramref name="issuer"/>.
    /// </summary>
    bool IsValidAudience(string issuer, IEnumerable<string> audiences);

    /// <summary>
    /// Validates token audiences for the resolved SaaS tenant using issuer, optional
    /// <paramref name="azpOrAppId"/>, and <see cref="ResolveJwtBearerProvider"/> rules.
    /// </summary>
    bool IsJwtAudienceValidForTenant(
        string issuer,
        IEnumerable<string> tokenAudiences,
        Guid resolvedTenantId,
        string? azpOrAppId);

    /// <summary>
    /// Validates that at least one registered provider accepts the token audiences for the
    /// issuer when no SaaS tenant context is available (e.g. anonymous tooling).
    /// </summary>
    bool IsJwtAudienceValidForIssuerAnyTenant(string issuer, IEnumerable<string> tokenAudiences);

    /// <summary>
    /// Find the provider matching the SHA-256 hash of a presented <c>X-Api-Key</c> header.
    /// The raw key should be hashed via <see cref="TenantApiKeyHasher.Hash(string)"/> before
    /// being passed in so callers cannot drift from the canonical hashing rule.
    /// </summary>
    /// <param name="hashedKey">Lower-case hex digest of the SHA-256 of the raw key.</param>
    TenantAuthProvider? GetByApiKeyHash(string hashedKey);

    /// <summary>
    /// Find the provider whose <see cref="TenantAuthProvider.CertificateThumbprint"/> matches
    /// the presented client certificate.
    /// </summary>
    TenantAuthProvider? GetByCertificateThumbprint(string thumbprint);

    /// <summary>
    /// Snapshot of all providers (used for diagnostics and tests).
    /// </summary>
    IReadOnlyCollection<TenantAuthProvider> GetAll();
}
