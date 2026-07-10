using System.Diagnostics.CodeAnalysis;
using GovUK.Dfe.ExternalApplications.Api.Client.Security;
using GovUK.Dfe.ExternalApplications.Api.Client.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Extensions;

/// <summary>
/// Registers shared External Applications API client infrastructure once per application.
/// </summary>
[ExcludeFromCodeCoverage]
public static class ExternalApplicationsApiClientRegistrar
{
    /// <summary>
    /// Registers handlers, token services, and optional token-exchange infrastructure.
    /// </summary>
    public static void RegisterInfrastructure(
        IServiceCollection services,
        IConfiguration configuration,
        bool? enableTokenExchange = null)
    {
        if (services.Any(d => d.ServiceType == typeof(ITokenAcquisitionService)))
        {
            return;
        }

        if (!services.Any(d => d.ServiceType == typeof(IApiClientSettingsProvider)))
        {
            services.AddSingleton<IApiClientSettingsProvider>(
                _ => new ConfigurationApiClientSettingsProvider(configuration));
        }

        var startupSettings = ConfigurationApiClientSettingsProvider.BindSettings(configuration);
        var useTokenExchange = enableTokenExchange ?? startupSettings.RequestTokenExchange;

        services.AddScoped<ITokenAcquisitionService, TokenAcquisitionService>();
        services.AddTransient<ApiClientRequestConfigurationHandler>();
        services.AddTransient<HeaderForwardingHandler>();
        services.AddTransient<AzureBearerTokenHandler>();

        if (useTokenExchange)
        {
            if (!services.Any(x => x.ServiceType == typeof(IDistributedCache)))
            {
                services.AddMemoryCache();
                services.AddDistributedMemoryCache();
            }

            services.AddScoped<IInternalUserTokenStore, CachedInternalUserTokenStore>();
            services.AddScoped<ICacheManager, DistributedCacheManager>();
            services.AddScoped<ITokenStateManager, TokenStateManager>();
            services.AddTransient<TokenExchangeHandler>(serviceProvider =>
                new TokenExchangeHandler(
                    serviceProvider.GetRequiredService<IHttpContextAccessor>(),
                    serviceProvider.GetRequiredService<IInternalUserTokenStore>(),
                    serviceProvider.GetRequiredService<Contracts.ITokensClient>(),
                    serviceProvider.GetRequiredService<ITokenAcquisitionService>(),
                    serviceProvider.GetRequiredService<ITokenStateManager>(),
                    serviceProvider.GetRequiredService<ILogger<TokenExchangeHandler>>()));

            services.AddTransient<UserAutoRegistrationHandler>(serviceProvider =>
                new UserAutoRegistrationHandler(
                    serviceProvider.GetRequiredService<IHttpClientFactory>(),
                    serviceProvider.GetRequiredService<ITokenStateManager>(),
                    serviceProvider.GetRequiredService<ITokenAcquisitionService>(),
                    serviceProvider.GetRequiredService<IHttpContextAccessor>(),
                    serviceProvider.GetRequiredService<IApiClientSettingsProvider>(),
                    serviceProvider.GetRequiredService<ILogger<UserAutoRegistrationHandler>>()));
        }
    }
}
