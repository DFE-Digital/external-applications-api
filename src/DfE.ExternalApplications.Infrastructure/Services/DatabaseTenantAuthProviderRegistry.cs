using System.Collections.Concurrent;
using System.Text;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace DfE.ExternalApplications.Infrastructure.Services;

/// <summary>
/// Singleton, hot-reloadable registry of <see cref="TenantAuthProvider"/> rows projected from
/// every tenant's settings in <see cref="ITenantConfigurationProvider"/>. Subscribes to
/// <see cref="ITenantConfigurationChangedNotifier.Changed"/> and rebuilds its indexes in-place,
/// so adding a tenant or rotating a signing key requires no service restart.
/// <para>
/// One <see cref="ConfigurationManager{T}"/> is held per OIDC issuer for JWKS auto-refresh.
/// Symmetric (HMAC) signing keys for the internal <c>JwtHmac</c> tokens are cached directly.
/// </para>
/// </summary>
public sealed class DatabaseTenantAuthProviderRegistry : ITenantAuthProviderRegistry, IDisposable
{
    private readonly ITenantConfigurationProvider _tenantConfigurationProvider;
    private readonly ITenantConfigurationChangedNotifier _notifier;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<DatabaseTenantAuthProviderRegistry> _logger;

    private volatile IReadOnlyDictionary<string, TenantAuthProvider> _byIssuer
        = new Dictionary<string, TenantAuthProvider>(StringComparer.OrdinalIgnoreCase);

    private volatile IReadOnlyDictionary<string, TenantAuthProvider> _byApiKeyHash
        = new Dictionary<string, TenantAuthProvider>(StringComparer.OrdinalIgnoreCase);

    private volatile IReadOnlyDictionary<string, TenantAuthProvider> _byThumbprint
        = new Dictionary<string, TenantAuthProvider>(StringComparer.OrdinalIgnoreCase);

    private volatile IReadOnlyCollection<TenantAuthProvider> _all = Array.Empty<TenantAuthProvider>();

    // Per-issuer OIDC ConfigurationManagers for JWKS auto-refresh. Created lazily, kept across
    // rebuilds so we don't tear down JWKS caches on every refresh.
    private readonly ConcurrentDictionary<string, ConfigurationManager<OpenIdConnectConfiguration>> _oidcConfigManagers
        = new(StringComparer.OrdinalIgnoreCase);

    public DatabaseTenantAuthProviderRegistry(
        ITenantConfigurationProvider tenantConfigurationProvider,
        ITenantConfigurationChangedNotifier notifier,
        IHttpClientFactory httpClientFactory,
        ILogger<DatabaseTenantAuthProviderRegistry> logger)
    {
        _tenantConfigurationProvider = tenantConfigurationProvider;
        _notifier = notifier;
        _httpClientFactory = httpClientFactory;
        _logger = logger;

        _notifier.Changed += Rebuild;

        // Initial build (synchronous - the configuration provider's startup is already complete by
        // the time DI resolves this registry).
        Rebuild();
    }

    /// <inheritdoc />
    public TenantAuthProvider? GetByIssuer(string issuer)
        => _byIssuer.TryGetValue(issuer, out var provider) ? provider : null;

    /// <inheritdoc />
    public TenantAuthProvider? GetByApiKeyHash(string hashedKey)
        => _byApiKeyHash.TryGetValue(hashedKey, out var provider) ? provider : null;

    /// <inheritdoc />
    public TenantAuthProvider? GetByCertificateThumbprint(string thumbprint)
        => _byThumbprint.TryGetValue(NormalizeThumbprint(thumbprint), out var provider) ? provider : null;

    /// <inheritdoc />
    public IReadOnlyCollection<TenantAuthProvider> GetAll() => _all;

    /// <inheritdoc />
    public async Task<IReadOnlyCollection<SecurityKey>> GetSigningKeysAsync(string issuer, CancellationToken cancellationToken)
    {
        var provider = GetByIssuer(issuer);
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

    /// <inheritdoc />
    public bool IsValidAudience(string issuer, IEnumerable<string> audiences)
    {
        var provider = GetByIssuer(issuer);
        if (provider?.Audiences is null || provider.Audiences.Count == 0)
        {
            return false;
        }

        foreach (var aud in audiences)
        {
            if (provider.Audiences.Contains(aud))
            {
                return true;
            }
        }
        return false;
    }

    private void Rebuild()
    {
        try
        {
            var tenants = _tenantConfigurationProvider.GetAllTenants();

            var byIssuer = new Dictionary<string, TenantAuthProvider>(StringComparer.OrdinalIgnoreCase);
            var byApiKey = new Dictionary<string, TenantAuthProvider>(StringComparer.OrdinalIgnoreCase);
            var byThumb = new Dictionary<string, TenantAuthProvider>(StringComparer.OrdinalIgnoreCase);
            var all = new List<TenantAuthProvider>();

            foreach (var tenant in tenants)
            {
                foreach (var provider in ProjectTenantProviders(tenant))
                {
                    all.Add(provider);

                    if (!string.IsNullOrEmpty(provider.Issuer))
                    {
                        byIssuer[provider.Issuer] = provider;
                    }

                    if (!string.IsNullOrEmpty(provider.ApiKeyHash))
                    {
                        byApiKey[provider.ApiKeyHash] = provider;
                    }

                    if (!string.IsNullOrEmpty(provider.CertificateThumbprint))
                    {
                        byThumb[NormalizeThumbprint(provider.CertificateThumbprint)] = provider;
                    }
                }
            }

            _byIssuer = byIssuer;
            _byApiKeyHash = byApiKey;
            _byThumbprint = byThumb;
            _all = all;

            _logger.LogInformation(
                "TenantAuthProviderRegistry rebuilt: {ProviderCount} providers across {TenantCount} tenants " +
                "({ByIssuer} by issuer, {ByApiKey} by api-key, {ByThumbprint} by cert)",
                all.Count, tenants.Count, byIssuer.Count, byApiKey.Count, byThumb.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to rebuild TenantAuthProviderRegistry. Retaining previous snapshot.");
        }
    }

    /// <summary>
    /// Projects a tenant's settings sections into a flat list of <see cref="TenantAuthProvider"/>.
    /// Prefers the explicit <c>AuthProviders</c> category when it contains at least one
    /// entry; otherwise falls back to the legacy <c>DfESignIn</c>/<c>EntraSso</c>/<c>AzureAd</c>/
    /// <c>Authorization:TokenSettings</c> sections so deployments can adopt the new shape lazily.
    /// </summary>
    private static IEnumerable<TenantAuthProvider> ProjectTenantProviders(TenantConfiguration tenant)
    {
        // 1) Preferred: explicit AuthProviders array.
        var explicitProviders = tenant.Settings.GetSection("AuthProviders:Providers");
        var explicitChildren = explicitProviders.GetChildren().ToArray();
        if (explicitChildren.Length > 0)
        {
            foreach (var providerSection in explicitChildren)
            {
                var projected = TryProjectExplicit(tenant.Id, providerSection);
                if (projected is not null)
                {
                    yield return projected;
                }
            }
            yield break;
        }

        // 2) Legacy projection (back-compat).
        // Internal HMAC-signed JWT (used by /tokens/exchange) - one per tenant.
        var internalKey = tenant.Settings["Authorization:TokenSettings:SecretKey"];
        var internalIssuer = tenant.Settings["Authorization:TokenSettings:Issuer"];
        var internalAudience = tenant.Settings["Authorization:TokenSettings:Audience"];
        if (!string.IsNullOrEmpty(internalKey) && !string.IsNullOrEmpty(internalIssuer))
        {
            yield return new TenantAuthProvider(
                TenantId: tenant.Id,
                Name: "internal-user",
                Kind: TenantAuthProviderKind.JwtHmac,
                IsServicePrincipal: false,
                Issuer: internalIssuer,
                Audiences: string.IsNullOrEmpty(internalAudience)
                    ? Array.Empty<string>()
                    : new[] { internalAudience },
                SigningKey: internalKey);
        }

        // DfE Sign-In (generic OIDC, user-facing).
        var dsiIssuer = tenant.Settings["DfESignIn:Issuer"]
            ?? tenant.Settings["DfESignIn:Authority"];
        var dsiAuthority = tenant.Settings["DfESignIn:Authority"];
        var dsiClientId = tenant.Settings["DfESignIn:ClientId"];
        var dsiDiscovery = tenant.Settings["DfESignIn:DiscoveryEndpoint"]
            ?? (string.IsNullOrEmpty(dsiAuthority) ? null : dsiAuthority.TrimEnd('/') + "/.well-known/openid-configuration");
        if (!string.IsNullOrEmpty(dsiIssuer))
        {
            yield return new TenantAuthProvider(
                TenantId: tenant.Id,
                Name: "dsi",
                Kind: TenantAuthProviderKind.Oidc,
                IsServicePrincipal: false,
                Issuer: dsiIssuer,
                Authority: dsiAuthority,
                DiscoveryEndpoint: dsiDiscovery,
                ClientId: dsiClientId,
                Audiences: string.IsNullOrEmpty(dsiClientId) ? Array.Empty<string>() : new[] { dsiClientId });
        }

        // Entra SSO (Entra OIDC, user-facing).
        var entraSsoTenant = tenant.Settings["EntraSso:TenantId"];
        var entraSsoClient = tenant.Settings["EntraSso:ClientId"];
        var entraSsoIssuer = tenant.Settings["EntraSso:Issuer"]
            ?? (string.IsNullOrEmpty(entraSsoTenant) ? null : $"https://login.microsoftonline.com/{entraSsoTenant}/v2.0");
        var entraSsoDiscovery = tenant.Settings["EntraSso:DiscoveryEndpoint"]
            ?? (string.IsNullOrEmpty(entraSsoTenant) ? null : $"https://login.microsoftonline.com/{entraSsoTenant}/v2.0/.well-known/openid-configuration");
        if (!string.IsNullOrEmpty(entraSsoIssuer))
        {
            yield return new TenantAuthProvider(
                TenantId: tenant.Id,
                Name: "entra-sso",
                Kind: TenantAuthProviderKind.EntraOidc,
                IsServicePrincipal: false,
                Issuer: entraSsoIssuer,
                DiscoveryEndpoint: entraSsoDiscovery,
                ClientId: entraSsoClient,
                Audiences: string.IsNullOrEmpty(entraSsoClient) ? Array.Empty<string>() : new[] { entraSsoClient });
        }

        // Azure AD (Entra app, service-to-service callers).
        var azureAdTenant = tenant.Settings["AzureAd:TenantId"];
        var azureAdAudience = tenant.Settings["AzureAd:Audience"]
            ?? tenant.Settings["AzureAd:ClientId"];
        var azureAdIssuer = tenant.Settings["AzureAd:Issuer"]
            ?? (string.IsNullOrEmpty(azureAdTenant) ? null : $"https://sts.windows.net/{azureAdTenant}/");
        var azureAdDiscovery = tenant.Settings["AzureAd:DiscoveryEndpoint"]
            ?? (string.IsNullOrEmpty(azureAdTenant) ? null : $"https://login.microsoftonline.com/{azureAdTenant}/v2.0/.well-known/openid-configuration");
        if (!string.IsNullOrEmpty(azureAdIssuer))
        {
            yield return new TenantAuthProvider(
                TenantId: tenant.Id,
                Name: "azure-ad-svc",
                Kind: TenantAuthProviderKind.EntraOidc,
                IsServicePrincipal: true,
                Issuer: azureAdIssuer,
                DiscoveryEndpoint: azureAdDiscovery,
                ClientId: tenant.Settings["AzureAd:ClientId"],
                Audiences: string.IsNullOrEmpty(azureAdAudience) ? Array.Empty<string>() : new[] { azureAdAudience });
        }
    }

    /// <summary>
    /// Projects a single entry from the explicit <c>AuthProviders:Providers:n</c> section into a
    /// <see cref="TenantAuthProvider"/>. Returns null for entries with an unknown or missing
    /// <c>Kind</c> (logged at the call site via the registry's existing error path).
    /// </summary>
    private static TenantAuthProvider? TryProjectExplicit(Guid tenantId, Microsoft.Extensions.Configuration.IConfigurationSection section)
    {
        var kindStr = section["Kind"];
        if (!Enum.TryParse<TenantAuthProviderKind>(kindStr, ignoreCase: true, out var kind))
        {
            return null;
        }

        var name = section["Name"] ?? kindStr!;
        var isSvc = bool.TryParse(section["IsServicePrincipal"], out var svc) && svc;
        var audiences = section.GetSection("Audiences").Get<string[]>()
            ?? (string.IsNullOrEmpty(section["Audience"])
                ? Array.Empty<string>()
                : new[] { section["Audience"]! });
        var roles = section.GetSection("Roles").Get<string[]>();

        return kind switch
        {
            TenantAuthProviderKind.JwtHmac => new TenantAuthProvider(
                TenantId: tenantId,
                Name: name,
                Kind: kind,
                IsServicePrincipal: isSvc,
                Issuer: section["Issuer"],
                Audiences: audiences,
                SigningKey: section["SigningKey"],
                Roles: roles),

            TenantAuthProviderKind.Oidc or TenantAuthProviderKind.EntraOidc => new TenantAuthProvider(
                TenantId: tenantId,
                Name: name,
                Kind: kind,
                IsServicePrincipal: isSvc,
                Issuer: section["Issuer"],
                Authority: section["Authority"],
                DiscoveryEndpoint: section["DiscoveryEndpoint"]
                    ?? (string.IsNullOrEmpty(section["Authority"])
                        ? null
                        : section["Authority"]!.TrimEnd('/') + "/.well-known/openid-configuration"),
                ClientId: section["ClientId"],
                Audiences: audiences,
                Roles: roles),

            TenantAuthProviderKind.ApiKey => new TenantAuthProvider(
                TenantId: tenantId,
                Name: name,
                Kind: kind,
                IsServicePrincipal: isSvc,
                ApiKeyHash: section["KeyHash"],
                Roles: roles),

            TenantAuthProviderKind.Mtls => new TenantAuthProvider(
                TenantId: tenantId,
                Name: name,
                Kind: kind,
                IsServicePrincipal: isSvc,
                CertificateThumbprint: section["Thumbprint"],
                Roles: roles),

            _ => null
        };
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

    /// <summary>
    /// Normalizes a certificate thumbprint to uppercase hex without separators so lookups are stable.
    /// </summary>
    private static string NormalizeThumbprint(string thumbprint)
        => thumbprint.Replace(":", "").Replace(" ", "").ToUpperInvariant();

    public void Dispose()
    {
        _notifier.Changed -= Rebuild;
    }
}
