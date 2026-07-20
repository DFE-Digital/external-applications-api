using Microsoft.IdentityModel.Tokens;

namespace GovUK.Dfe.FlexForms.Infrastructure.Security;

/// <summary>
/// Resolves the set of <see cref="SecurityKey"/>s used to validate JWT bearer tokens for a
/// given <paramref name="issuer"/>. Lives in the Infrastructure layer because <c>SecurityKey</c>
/// and the OIDC <c>ConfigurationManager</c> cache are framework concerns; the Domain registry
/// (<c>ITenantAuthProviderRegistry</c>) deliberately stays free of <c>Microsoft.IdentityModel.*</c>
/// types.
/// <para>
/// For <c>JwtHmac</c> providers this returns one or more <see cref="SymmetricSecurityKey"/>s built
/// from the per-tenant signing secret(s). For <c>Oidc</c> / <c>EntraOidc</c> providers this returns
/// the JWKS keys fetched (and cached / auto-refreshed) by a <c>ConfigurationManager</c>
/// pinned per discovery endpoint.
/// </para>
/// </summary>
public interface ITenantSigningKeyResolver
{
    /// <summary>
    /// Resolve the active signing keys for the provider that owns <paramref name="issuer"/>,
    /// or an empty collection when no provider matches.
    /// </summary>
    Task<IReadOnlyCollection<SecurityKey>> GetSigningKeysAsync(string issuer, CancellationToken cancellationToken);
}
