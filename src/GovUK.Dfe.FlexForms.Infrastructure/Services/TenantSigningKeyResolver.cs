using System.Collections.Concurrent;
using System.Text;
using GovUK.Dfe.FlexForms.Domain.Tenancy;
using GovUK.Dfe.FlexForms.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace GovUK.Dfe.FlexForms.Infrastructure.Services;

/// <summary>
/// Default implementation of <see cref="ITenantSigningKeyResolver"/>. Singleton: it owns a
/// long-lived <see cref="ConfigurationManager{T}"/> per OIDC discovery endpoint so JWKS auto-
/// refresh works without re-creating the manager on every request.
/// <para>
/// Reads from <see cref="ITenantAuthProviderRegistry"/> for the underlying provider record; the
/// registry itself is unaware of <see cref="SecurityKey"/>, keeping the Domain free of any
/// JWT-framework types.
/// </para>
/// </summary>
public sealed class TenantSigningKeyResolver : ITenantSigningKeyResolver
{
    private readonly ITenantAuthProviderRegistry _registry;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<TenantSigningKeyResolver> _logger;

    private readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _oidcConfigManagers
        = new(StringComparer.OrdinalIgnoreCase);

    public TenantSigningKeyResolver(
        ITenantAuthProviderRegistry registry,
        IHttpClientFactory httpClientFactory,
        ILogger<TenantSigningKeyResolver> logger)
    {
        _registry = registry;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<SecurityKey>> GetSigningKeysAsync(string issuer, CancellationToken cancellationToken)
    {
        var providers = _registry.GetProvidersByIssuer(issuer);
        if (providers.Count == 0)
        {
            return Array.Empty<SecurityKey>();
        }

        var keys = new List<SecurityKey>();

        // Prefer all HMAC keys for this issuer (internal user JWTs). TryAllIssuerSigningKeys
        // will attempt each when the token has no kid.
        foreach (var provider in providers.Where(p => p.Kind == TenantAuthProviderKind.JwtHmac))
        {
            if (string.IsNullOrEmpty(provider.SigningKey))
            {
                continue;
            }

            var keyBytes = Encoding.UTF8.GetBytes(provider.SigningKey);
            if (keyBytes.Length < 32)
            {
                _logger.LogWarning(
                    "JwtHmac signing key for issuer {Issuer} (tenant {TenantId}) is {ByteLength} bytes; " +
                    "HS256 requires at least 32. Token validation will fail under IdentityModel 8+.",
                    issuer,
                    provider.TenantId,
                    keyBytes.Length);
            }

            keys.Add(new SymmetricSecurityKey(keyBytes)
            {
                // Stable id so handlers that match on kid can resolve this key; tokens minted
                // without kid still validate when TryAllIssuerSigningKeys is true.
                KeyId = $"hmac:{provider.TenantId:D}"
            });
        }

        if (keys.Count > 0)
        {
            return keys;
        }

        // Fall back to OIDC / Entra JWKS when no HMAC provider owns this issuer.
        var oidcProvider = providers.FirstOrDefault(p =>
            p.Kind is TenantAuthProviderKind.Oidc or TenantAuthProviderKind.EntraOidc
            && !string.IsNullOrEmpty(p.DiscoveryEndpoint));

        if (oidcProvider is null)
        {
            return Array.Empty<SecurityKey>();
        }

        var manager = GetOrCreateConfigManager(oidcProvider);
        if (manager is null)
        {
            return Array.Empty<SecurityKey>();
        }

        try
        {
            var config = await manager.GetConfigurationAsync(cancellationToken);
            return config.SigningKeys.ToArray();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Failed to fetch OIDC signing keys for issuer {Issuer} (tenant {TenantId}).",
                issuer, oidcProvider.TenantId);
            return Array.Empty<SecurityKey>();
        }
    }

    private ConfigurationManager<OpenIdConnectConfiguration>? GetOrCreateConfigManager(TenantAuthProvider provider)
    {
        var endpoint = provider.DiscoveryEndpoint;
        if (string.IsNullOrEmpty(endpoint))
        {
            return null;
        }

        return _oidcConfigManagers.GetOrAdd(endpoint, ep => new ConfigurationManager<OpenIdConnectConfiguration>(
            metadataAddress: ep,
            configRetriever: new OpenIdConnectConfigurationRetriever(),
            docRetriever: new HttpDocumentRetriever(_httpClientFactory.CreateClient())
            {
                RequireHttps = ep.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            }));
    }
}
