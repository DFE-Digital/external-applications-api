using Microsoft.IdentityModel.Tokens;

namespace DfE.ExternalApplications.Domain.Tenancy;

/// <summary>
/// Hot-reloadable, in-memory registry of <see cref="TenantAuthProvider"/> rows projected from
/// the tenant configuration database. Indexed by <c>Issuer</c>, <c>ApiKeyHash</c> and
/// <c>CertificateThumbprint</c> so each authentication scheme can resolve the matching provider
/// in O(1) per request.
/// <para>
/// The registry MUST subscribe to <see cref="ITenantConfigurationChangedNotifier.Changed"/> and
/// rebuild its indexes when notified, so adding a new tenant or rotating a signing key is a
/// pure DB write picked up within the configured refresh interval - no service restart.
/// </para>
/// </summary>
public interface ITenantAuthProviderRegistry
{
    /// <summary>
    /// Find the auth provider that issues tokens with the given <paramref name="issuer"/> claim.
    /// </summary>
    /// <param name="issuer">Value of the <c>iss</c> claim.</param>
    /// <returns>The matching provider, or <c>null</c> if no tenant claims this issuer.</returns>
    TenantAuthProvider? GetByIssuer(string issuer);

    /// <summary>
    /// Resolve the set of signing keys to validate tokens minted by <paramref name="issuer"/>.
    /// For OIDC providers this returns the JWKS keys (cached via <c>ConfigurationManager</c>);
    /// for <c>JwtHmac</c> providers this returns a single <see cref="SymmetricSecurityKey"/>.
    /// </summary>
    /// <param name="issuer">Value of the <c>iss</c> claim.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task<IReadOnlyCollection<SecurityKey>> GetSigningKeysAsync(string issuer, CancellationToken cancellationToken);

    /// <summary>
    /// Validate that at least one of <paramref name="audiences"/> matches the audience(s)
    /// configured for the provider with the given <paramref name="issuer"/>.
    /// </summary>
    bool IsValidAudience(string issuer, IEnumerable<string> audiences);

    /// <summary>
    /// Find the provider matching the SHA-256 hash of a presented <c>X-Api-Key</c> header.
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
