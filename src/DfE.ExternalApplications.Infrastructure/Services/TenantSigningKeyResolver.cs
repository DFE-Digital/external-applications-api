using System.Collections.Concurrent;
using System.Text;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Infrastructure.Security;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace DfE.ExternalApplications.Infrastructure.Services;

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
        var provider = _registry.GetFirstSigningProviderForIssuer(issuer);
        if (provider is null)
        {
            return Array.Empty<SecurityKey>();
        }

        switch (provider.Kind)
        {
            case TenantAuthProviderKind.JwtHmac:
                if (string.IsNullOrEmpty(provider.SigningKey))
                {
                    return Array.Empty<SecurityKey>();
                }
                return new SecurityKey[]
                {
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(provider.SigningKey))
                };

            case TenantAuthProviderKind.Oidc:
            case TenantAuthProviderKind.EntraOidc:
                var manager = GetOrCreateConfigManager(provider);
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
                        issuer, provider.TenantId);
                    return Array.Empty<SecurityKey>();
                }

            default:
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
