using System.Diagnostics.CodeAnalysis;
using DfE.ExternalApplications.Api.Client.Security;
using DfE.ExternalApplications.Api.Client.Settings;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DfE.ExternalApplications.Api.Client.Extensions
{
    [ExcludeFromCodeCoverage]
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddApiClient<TClientInterface, TClientImplementation>(
            this IServiceCollection services,
            IConfiguration configuration,
            HttpClient? existingHttpClient = null)
            where TClientInterface : class
            where TClientImplementation : class, TClientInterface
        {
            var apiSettings = new ApiClientSettings();
            configuration.GetSection("ApiClient").Bind(apiSettings);

            services.AddSingleton(apiSettings);
            services.AddSingleton<ITokenAcquisitionService, TokenAcquisitionService>();

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
                services.AddHttpClient<TClientInterface, TClientImplementation>((httpClient, serviceProvider) =>
                {
                    httpClient.BaseAddress = new Uri(apiSettings.BaseUrl!);

                    return ActivatorUtilities.CreateInstance<TClientImplementation>(
                        serviceProvider, httpClient, apiSettings.BaseUrl!);
                })
                .AddHttpMessageHandler(serviceProvider =>
                {
                    var tokenService = serviceProvider.GetRequiredService<ITokenAcquisitionService>();
                    return new BearerTokenHandler(tokenService);
                });
            }
            return services;
        }
    }
}
