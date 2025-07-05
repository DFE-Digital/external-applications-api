using System.Diagnostics.CodeAnalysis;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Security;

[ExcludeFromCodeCoverage]
public class AzureBearerTokenHandler(
    ITokenAcquisitionService tokenAcquisitionService,
    ILogger<AzureBearerTokenHandler> logger)
    : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        // Always use Azure token for tokens client authentication
        logger.LogDebug("Getting Azure token for tokens client authentication");
        var azureToken = await tokenAcquisitionService.GetTokenAsync();
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", azureToken);
        
        return await base.SendAsync(request, cancellationToken);
    }
} 