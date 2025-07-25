using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;
using GovUK.Dfe.ExternalApplications.Api.Client.Security;
using GovUK.Dfe.ExternalApplications.Api.Client.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddExternalApplicationsApiClient<TClientInterface, TClientImplementation>(
            this IServiceCollection services,
            IConfiguration configuration,
            HttpClient? existingHttpClient = null)
            where TClientInterface : class
            where TClientImplementation : class, TClientInterface
        {
            var apiSettings = new ApiClientSettings();
            configuration.GetSection("ExternalApplicationsApiClient").Bind(apiSettings);

            services.AddSingleton(apiSettings);
            services.AddSingleton<ITokenAcquisitionService, TokenAcquisitionService>();
            services.AddHttpContextAccessor();
            
            // Register handlers
            services.AddTransient<AzureBearerTokenHandler>();
            
            if (apiSettings.RequestTokenExchange)
            {
                // Frontend clients need internal token storage and exchange handler
                services.AddScoped<IInternalUserTokenStore, CachedInternalUserTokenStore>();
                services.AddTransient<TokenExchangeHandler>(serviceProvider =>
                {
                    return new TokenExchangeHandler(
                        serviceProvider.GetRequiredService<IHttpContextAccessor>(),
                        serviceProvider.GetRequiredService<IInternalUserTokenStore>(),
                        serviceProvider.GetRequiredService<ITokensClient>(),
                        serviceProvider.GetRequiredService<ITokenAcquisitionService>(),
                        serviceProvider.GetRequiredService<ILogger<TokenExchangeHandler>>());
                });
            }

            if (existingHttpClient != null)
            {
                services.AddSingleton(existingHttpClient);
                services.AddTransient<TClientInterface, TClientImplementation>(serviceProvider =>
                {
                    return ActivatorUtilities.CreateInstance<TClientImplementation>(
                        serviceProvider, existingHttpClient, apiSettings.BaseUrl!);
                });
            }
            else
            {
                var builder = services.AddHttpClient<TClientInterface, TClientImplementation>((httpClient, serviceProvider) =>
                {
                    httpClient.BaseAddress = new Uri(apiSettings.BaseUrl!);

                    return ActivatorUtilities.CreateInstance<TClientImplementation>(
                        serviceProvider, httpClient, apiSettings.BaseUrl!);
                });

                if (apiSettings.RequestTokenExchange)
                {
                    // Frontend clients: Use exchange flow
                    if (typeof(TClientInterface) == typeof(ITokensClient))
                    {
                        // Tokens client always uses Azure token (for exchange endpoint authentication)
                        builder.AddHttpMessageHandler<AzureBearerTokenHandler>();
                    }
                    else
                    {
                        // Other clients use token exchange to get internal tokens
                        builder.AddHttpMessageHandler<TokenExchangeHandler>();
                    }
                }
                else
                {
                    // Service clients: Use Azure token for everything (no exchange needed)
                    builder.AddHttpMessageHandler<AzureBearerTokenHandler>();
                }
            }

            return services;
        }
    }
}