using System;
using System.Linq;
using System.Net.Http;
using System.Diagnostics.CodeAnalysis;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;
using GovUK.Dfe.ExternalApplications.Api.Client.Security;
using GovUK.Dfe.ExternalApplications.Api.Client.Settings;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddExternalApplicationsApiClient<TClientInterface, TClientImplementation>(
            this IServiceCollection services,
            IConfiguration configuration,
            HttpClient? existingHttpClient = null,
            bool? enableTokenExchange = null)
            where TClientInterface : class
            where TClientImplementation : class, TClientInterface
        {
            ExternalApplicationsApiClientRegistrar.RegisterInfrastructure(
                services,
                configuration,
                enableTokenExchange);

            services.AddHttpContextAccessor();

            var startupSettings = ConfigurationApiClientSettingsProvider.BindSettings(configuration);
            var useTokenExchange = enableTokenExchange ?? startupSettings.RequestTokenExchange;
            var fallbackBaseUrl = startupSettings.BaseUrl ?? "https://localhost/";

            if (existingHttpClient != null)
            {
                services.AddSingleton(existingHttpClient);
                services.AddTransient<TClientInterface, TClientImplementation>(serviceProvider =>
                    ActivatorUtilities.CreateInstance<TClientImplementation>(
                        serviceProvider, existingHttpClient, fallbackBaseUrl));
            }
            else
            {
                var builder = services.AddHttpClient<TClientInterface, TClientImplementation>((httpClient, serviceProvider) =>
                {
                    httpClient.BaseAddress = new Uri(fallbackBaseUrl);

                    return ActivatorUtilities.CreateInstance<TClientImplementation>(
                        serviceProvider, httpClient, fallbackBaseUrl);
                });

                builder.AddHttpMessageHandler<ApiClientRequestConfigurationHandler>();
                builder.AddHttpMessageHandler<HeaderForwardingHandler>();

                if (useTokenExchange)
                {
                    if (typeof(TClientInterface) == typeof(ITokensClient))
                    {
                        builder.AddHttpMessageHandler<AzureBearerTokenHandler>();

                        if (startupSettings.AutoRegisterUsers)
                        {
                            builder.AddHttpMessageHandler<UserAutoRegistrationHandler>();
                        }
                    }
                    else
                    {
                        builder.AddHttpMessageHandler<TokenExchangeHandler>();
                    }
                }
                else
                {
                    builder.AddHttpMessageHandler<AzureBearerTokenHandler>();
                }
            }

            return services;
        }

        /// <summary>
        /// Extension method to register the new token management middleware
        /// </summary>
        public static IApplicationBuilder UseTokenManagementMiddleware(this IApplicationBuilder app)
        {
            return app.UseMiddleware<TokenManagementMiddleware>();
        }
    }
}
