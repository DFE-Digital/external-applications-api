using DfE.CoreLibs.Http.Models;
using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;
using GovUK.Dfe.ExternalApplications.Api.Client.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Text;
using System.Text.Json;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Security;

[ExcludeFromCodeCoverage]
public class TokenExchangeHandler(
    IHttpContextAccessor httpContextAccessor,
    IInternalUserTokenStore tokenStore,
    ITokensClient tokensClient,
    ITokenAcquisitionService tokenAcquisitionService,
    ILogger<TokenExchangeHandler> logger)
    : DelegatingHandler
{
    // How close to expiration before we force logout
    private static readonly TimeSpan ExpiryBuffer = TimeSpan.FromMinutes(5);

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        
        var internalToken = tokenStore.GetToken();
        if (!string.IsNullOrEmpty(internalToken) && IsTokenValid(internalToken))
        {
            logger.LogDebug("Using cached internal token for API request");
            // We have a valid internal token, use it and continue
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", internalToken);
            return await base.SendAsync(request, cancellationToken);
        }

        try
        {
            // Get DSI token from authentication context
            var externalIdpToken = await httpContext?.GetTokenAsync("id_token");
            if (string.IsNullOrEmpty(externalIdpToken) || !IsTokenValid(externalIdpToken))
            {
                logger.LogWarning("No valid DSI token found");
                return UnauthorizedResponse(request);
            }

            // Azure token (for authorization to call exchange endpoint)
            var azureToken = await tokenAcquisitionService.GetTokenAsync();

            // Call exchange endpoint with DSI token in body
            var exchangeResult = await tokensClient.ExchangeAndStoreAsync(externalIdpToken, tokenStore, cancellationToken);
            
            if (string.IsNullOrEmpty(exchangeResult.AccessToken))
            {
                logger.LogDebug("Token exchange returned empty internal token");
                return UnauthorizedResponse(request);
            }

            // Use the new internal token for the current request
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", exchangeResult.AccessToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Token exchange process failed");
            return UnauthorizedResponse(request);
        }

        return await base.SendAsync(request, cancellationToken);
    }

    private static bool IsTokenValid(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo > DateTime.UtcNow.Add(ExpiryBuffer);
        }
        catch
        {
            return false;
        }
    }

    private static HttpResponseMessage UnauthorizedResponse(HttpRequestMessage request)
    {
        var payload = new ExceptionResponse
        {
            ErrorId = "TE-401",
            StatusCode = (int)HttpStatusCode.Unauthorized,
            Message = "Token exchange failed – user needs to re-authenticate",
            ExceptionType = "TokenExchangeException",
            Timestamp = DateTime.UtcNow
        };

        var json = JsonSerializer.Serialize(payload);

        return new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            RequestMessage = request,
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
            ReasonPhrase = "Token exchange failed"
        };
    }
}
