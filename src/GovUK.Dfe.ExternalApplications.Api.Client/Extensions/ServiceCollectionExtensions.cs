using System.Diagnostics.CodeAnalysis;
using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;
using GovUK.Dfe.ExternalApplications.Api.Client.Security;
using GovUK.Dfe.ExternalApplications.Api.Client.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
            services.AddSingleton<IInternalUserTokenStore, HttpContextInternalUserTokenStore>();
            if (apiSettings.RequestTokenExchange)
            {
                services.AddTransient<TokenExchangeHandler>();
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

                if (typeof(TClientInterface) != typeof(ITokensClient) && apiSettings.RequestTokenExchange)
                {
                    builder.AddHttpMessageHandler<TokenExchangeHandler>();
                }

                builder.AddHttpMessageHandler(serviceProvider =>
                {
                    var tokenService = serviceProvider.GetRequiredService<ITokenAcquisitionService>();
                    var tokenStore = serviceProvider.GetRequiredService<IInternalUserTokenStore>();

                    return new BearerTokenHandler(tokenService, tokenStore);
                });
            }
            return services;
        }
    }
}