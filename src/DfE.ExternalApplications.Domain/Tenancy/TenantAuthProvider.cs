using System.Collections.Generic;

namespace DfE.ExternalApplications.Domain.Tenancy;

/// <summary>
/// Data-driven description of a single authentication provider belonging to a tenant.
/// The registry projects per-tenant configuration into a flat list of these records
/// and then indexes them by <see cref="Issuer"/>, <see cref="ApiKeyHash"/> and
/// <see cref="CertificateThumbprint"/> so a request can be routed to the right
/// tenant without baking anything at startup.
/// </summary>
/// <param name="TenantId">The owning tenant id.</param>
/// <param name="Name">A short, tenant-local logical name (e.g. <c>"dsi"</c>, <c>"entra-svc"</c>).</param>
/// <param name="Kind">The provider kind (<c>Oidc</c>, <c>EntraOidc</c>, <c>JwtHmac</c>, <c>ApiKey</c>, <c>Mtls</c>).</param>
/// <param name="IsServicePrincipal">
/// <c>true</c> when this provider authenticates service-to-service callers (machine identity);
/// <c>false</c> when it authenticates an interactive end-user identity.
/// Used by the provider-agnostic <c>ServiceCallers</c> authorization policy.
/// </param>
/// <param name="Issuer">Token issuer (<c>iss</c>) - required for OIDC/JwtHmac, null for ApiKey/Mtls.</param>
/// <param name="Authority">OIDC authority URL used to build a <c>ConfigurationManager</c>. Optional for JwtHmac.</param>
/// <param name="DiscoveryEndpoint">Explicit .well-known endpoint (overrides <c>Authority</c> if set).</param>
/// <param name="Audiences">Valid audience values (<c>aud</c>) accepted from this provider's tokens.</param>
/// <param name="ClientId">The application/client id - used for Entra and OIDC.</param>
/// <param name="SigningKey">Symmetric signing key for <c>JwtHmac</c> providers (internal tokens).</param>
/// <param name="ApiKeyHash">SHA-256 hex digest of the shared API key for <c>ApiKey</c> providers.</param>
/// <param name="CertificateThumbprint">Allowed client-certificate thumbprint for <c>Mtls</c> providers.</param>
/// <param name="Roles">Static roles to project onto the principal for service identities.</param>
public sealed record TenantAuthProvider(
    Guid TenantId,
    string Name,
    TenantAuthProviderKind Kind,
    bool IsServicePrincipal,
    string? Issuer = null,
    string? Authority = null,
    string? DiscoveryEndpoint = null,
    IReadOnlyCollection<string>? Audiences = null,
    string? ClientId = null,
    string? SigningKey = null,
    string? ApiKeyHash = null,
    string? CertificateThumbprint = null,
    IReadOnlyCollection<string>? Roles = null);

/// <summary>
/// Discriminator for <see cref="TenantAuthProvider"/>. Drives which scheme picks up the provider
/// and which fields are required.
/// </summary>
public enum TenantAuthProviderKind
{
    /// <summary>Generic OIDC provider (e.g. DfE Sign-In).</summary>
    Oidc,
    /// <summary>Microsoft Entra OIDC (treated as <see cref="Oidc"/> with Entra-specific discovery).</summary>
    EntraOidc,
    /// <summary>Internal HMAC-signed JWT minted by this API (per-tenant signing key).</summary>
    JwtHmac,
    /// <summary>Shared-secret API key authentication (header lookup).</summary>
    ApiKey,
    /// <summary>Mutual TLS / client certificate authentication.</summary>
    Mtls,
}
