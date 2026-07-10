using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using GovUK.Dfe.ExternalApplications.Api.Client.Settings;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Security
{
    [ExcludeFromCodeCoverage]
    public class TokenAcquisitionService(
        IApiClientSettingsProvider settingsProvider,
        ILogger<TokenAcquisitionService> logger) : ITokenAcquisitionService
    {
        private readonly ConcurrentDictionary<string, Lazy<IConfidentialClientApplication>> _applications = new();

        public async Task<string> GetTokenAsync()
        {
            var settings = settingsProvider.GetSettings()
                ?? throw new InvalidOperationException("API client settings are not available.");

            if (string.IsNullOrWhiteSpace(settings.ClientId))
            {
                throw new InvalidOperationException("ExternalApplicationsApiClient:ClientId is not configured.");
            }

            if (string.IsNullOrWhiteSpace(settings.ClientSecret))
            {
                throw new InvalidOperationException("ExternalApplicationsApiClient:ClientSecret is not configured.");
            }

            if (string.IsNullOrWhiteSpace(settings.Authority))
            {
                throw new InvalidOperationException("ExternalApplicationsApiClient:Authority is not configured.");
            }

            if (string.IsNullOrWhiteSpace(settings.Scope))
            {
                throw new InvalidOperationException("ExternalApplicationsApiClient:Scope is not configured.");
            }

            var cacheKey = $"{settings.Authority}|{settings.ClientId}";
            var application = _applications.GetOrAdd(cacheKey, _ => new Lazy<IConfidentialClientApplication>(() =>
                ConfidentialClientApplicationBuilder.Create(settings.ClientId)
                    .WithClientSecret(settings.ClientSecret)
                    .WithAuthority(new Uri(settings.Authority))
                    .Build())).Value;

            var authResult = await application.AcquireTokenForClient(new[] { settings.Scope })
                .ExecuteAsync();

            if (authResult?.AccessToken is not { Length: > 0 } accessToken)
            {
                throw new InvalidOperationException("Token acquisition returned empty access token");
            }

            logger.LogDebug("Acquired client-credentials token for API client {ClientId}", settings.ClientId);
            return accessToken;
        }
    }
}
