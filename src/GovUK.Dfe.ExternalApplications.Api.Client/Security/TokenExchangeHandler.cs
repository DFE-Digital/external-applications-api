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
        logger.LogDebug(">>>>>>>>>> Authentication >>> TokenExchangeHandler.SendAsync started for request: {Method} {Uri}", 
            request.Method, request.RequestUri);

        var httpContext = httpContextAccessor.HttpContext;
        
        if (httpContext == null)
        {
            logger.LogError(">>>>>>>>>> Authentication >>> HttpContext is null in TokenExchangeHandler for request: {Method} {Uri}", 
                request.Method, request.RequestUri);
            return UnauthorizedResponse(request);
        }

        logger.LogDebug(">>>>>>>>>> Authentication >>> HttpContext available, User.Identity.IsAuthenticated: {IsAuthenticated}, User.Identity.Name: {UserName}", 
            httpContext.User?.Identity?.IsAuthenticated, httpContext.User?.Identity?.Name);
        
        // Get initial token status overview
        logger.LogInformation(">>>>>>>>>> Authentication >>> ===== TOKEN STATUS OVERVIEW FOR REQUEST: {Method} {Uri} =====", 
            request.Method, request.RequestUri);
        
        var internalToken = tokenStore.GetToken();
        logger.LogDebug(">>>>>>>>>> Authentication >>> Retrieved internal token from store, HasToken: {HasToken}", 
            !string.IsNullOrEmpty(internalToken));

        // Log internal token expiry if available
        if (!string.IsNullOrEmpty(internalToken))
        {
            var internalTokenExpiry = GetTokenExpiry(internalToken);
            var internalTokenTimeRemaining = internalTokenExpiry - DateTime.UtcNow;
            
            logger.LogInformation(">>>>>>>>>> Authentication >>> INTERNAL TOKEN EXPIRY: {ExpiryTime} UTC (Time remaining: {TimeRemaining})", 
                internalTokenExpiry, internalTokenTimeRemaining);

            var isValid = IsTokenValid(internalToken);
            logger.LogDebug(">>>>>>>>>> Authentication >>> Internal token validation result: {IsValid}, TokenLength: {TokenLength}", 
                isValid, internalToken.Length);

            if (isValid)
            {
                logger.LogInformation(">>>>>>>>>> Authentication >>> Using cached internal token for API request: {Method} {Uri}", 
                    request.Method, request.RequestUri);
                
                logger.LogInformation(">>>>>>>>>> Authentication >>> ===== USING CACHED TOKEN - NO EXCHANGE NEEDED =====");
                logger.LogInformation(">>>>>>>>>> Authentication >>> CACHED INTERNAL TOKEN EXPIRY: {ExpiryTime} UTC (Time remaining: {TimeRemaining})", 
                    internalTokenExpiry, internalTokenTimeRemaining);
                logger.LogInformation(">>>>>>>>>> Authentication >>> ===== PROCEEDING WITH CACHED TOKEN =====");
                
                // We have a valid internal token, use it and continue
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", internalToken);
                
                var response = await base.SendAsync(request, cancellationToken);
                
                logger.LogInformation(">>>>>>>>>> Authentication >>> Request with cached token completed with status: {StatusCode} for {Method} {Uri}", 
                    response.StatusCode, request.Method, request.RequestUri);

                if (response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden)
                {
                    logger.LogWarning(">>>>>>>>>> Authentication >>> Cached token resulted in {StatusCode} - token may be expired or invalid for {Method} {Uri}", 
                        response.StatusCode, request.Method, request.RequestUri);
                    
                    // Clear the token and try exchange
                    tokenStore.ClearToken();
                    logger.LogDebug(">>>>>>>>>> Authentication >>> Cleared invalid cached token, attempting token exchange");
                    
                    // Don't return the failed response, continue to token exchange
                }
                else
                {
                    return response;
                }
            }
            else
            {
                logger.LogDebug(">>>>>>>>>> Authentication >>> Cached internal token is invalid/expired, clearing and proceeding to exchange");
                tokenStore.ClearToken();
            }
        }
        else
        {
            logger.LogDebug(">>>>>>>>>> Authentication >>> No internal token in cache, proceeding to token exchange");
        }

        try
        {
            logger.LogDebug(">>>>>>>>>> Authentication >>> Starting token exchange process for request: {Method} {Uri}", 
                request.Method, request.RequestUri);

            // Get DSI token from authentication context
            logger.LogDebug(">>>>>>>>>> Authentication >>> Retrieving DSI id_token from authentication context");
            var externalIdpToken = await httpContext?.GetTokenAsync("id_token");
            
            if (string.IsNullOrEmpty(externalIdpToken))
            {
                logger.LogError(">>>>>>>>>> Authentication >>> No DSI id_token found in authentication context for user: {UserName}", 
                    httpContext?.User?.Identity?.Name);
                return UnauthorizedResponse(request);
            }

            logger.LogDebug(">>>>>>>>>> Authentication >>> DSI id_token retrieved, length: {TokenLength} chars", 
                externalIdpToken.Length);

            // Log ExternalIdpToken (DSI token) expiry
            var externalIdpTokenExpiry = GetTokenExpiry(externalIdpToken);
            var externalIdpTokenTimeRemaining = externalIdpTokenExpiry - DateTime.UtcNow;
            
            logger.LogInformation(">>>>>>>>>> Authentication >>> EXTERNAL IDP TOKEN (DSI) EXPIRY: {ExpiryTime} UTC (Time remaining: {TimeRemaining})", 
                externalIdpTokenExpiry, externalIdpTokenTimeRemaining);

            var isExternalTokenValid = IsTokenValid(externalIdpToken);
            logger.LogDebug(">>>>>>>>>> Authentication >>> DSI token validation result: {IsValid}", isExternalTokenValid);

            if (!isExternalTokenValid)
            {
                logger.LogWarning(">>>>>>>>>> Authentication >>> DSI id_token is invalid or expired for user: {UserName}", 
                    httpContext?.User?.Identity?.Name);
                return UnauthorizedResponse(request);
            }

            // Azure token (for authorization to call exchange endpoint)
            logger.LogDebug(">>>>>>>>>> Authentication >>> Getting Azure token for token exchange endpoint authorization");
            var azureToken = await tokenAcquisitionService.GetTokenAsync();
            
            if (string.IsNullOrEmpty(azureToken))
            {
                logger.LogError(">>>>>>>>>> Authentication >>> Failed to get Azure token for token exchange endpoint");
                return UnauthorizedResponse(request);
            }

            logger.LogDebug(">>>>>>>>>> Authentication >>> Azure token acquired for exchange endpoint, length: {TokenLength} chars", 
                azureToken.Length);

            // Log Azure token expiry
            var azureTokenExpiry = GetTokenExpiry(azureToken);
            var azureTokenTimeRemaining = azureTokenExpiry - DateTime.UtcNow;
            
            logger.LogInformation(">>>>>>>>>> Authentication >>> AZURE TOKEN EXPIRY: {ExpiryTime} UTC (Time remaining: {TimeRemaining})", 
                azureTokenExpiry, azureTokenTimeRemaining);

            // Call exchange endpoint with DSI token in body
            logger.LogInformation(">>>>>>>>>> Authentication >>> Calling token exchange endpoint with DSI token");
            var exchangeResult = await tokensClient.ExchangeAndStoreAsync(externalIdpToken, tokenStore, cancellationToken);
            
            if (exchangeResult == null)
            {
                logger.LogError(">>>>>>>>>> Authentication >>> Token exchange returned null result");
                return UnauthorizedResponse(request);
            }

            logger.LogDebug(">>>>>>>>>> Authentication >>> Token exchange completed, HasAccessToken: {HasAccessToken}", 
                !string.IsNullOrEmpty(exchangeResult.AccessToken));
            
            if (string.IsNullOrEmpty(exchangeResult.AccessToken))
            {
                logger.LogError(">>>>>>>>>> Authentication >>> Token exchange returned empty internal token for user: {UserName}", 
                    httpContext?.User?.Identity?.Name);
                return UnauthorizedResponse(request);
            }

            logger.LogInformation(">>>>>>>>>> Authentication >>> Token exchange successful, new internal token acquired, length: {TokenLength} chars", 
                exchangeResult.AccessToken.Length);

            // Log newly exchanged OBO token expiry
            var newInternalTokenExpiry = GetTokenExpiry(exchangeResult.AccessToken);
            var newInternalTokenTimeRemaining = newInternalTokenExpiry - DateTime.UtcNow;
            
            logger.LogInformation(">>>>>>>>>> Authentication >>> NEW EXCHANGED OBO TOKEN EXPIRY: {ExpiryTime} UTC (Time remaining: {TimeRemaining})", 
                newInternalTokenExpiry, newInternalTokenTimeRemaining);

            // Use the new internal token for the current request
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", exchangeResult.AccessToken);
            
            logger.LogDebug(">>>>>>>>>> Authentication >>> Authorization header set with new internal token for request: {Method} {Uri}", 
                request.Method, request.RequestUri);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, ">>>>>>>>>> Authentication >>> Token exchange process failed for request: {Method} {Uri}, User: {UserName}", 
                request.Method, request.RequestUri, httpContext?.User?.Identity?.Name);
            return UnauthorizedResponse(request);
        }

        logger.LogInformation(">>>>>>>>>> Authentication >>> ===== FINAL TOKEN SUMMARY BEFORE API CALL =====");
        logger.LogInformation(">>>>>>>>>> Authentication >>> Request: {Method} {Uri}", request.Method, request.RequestUri);
        logger.LogInformation(">>>>>>>>>> Authentication >>> All token expiry times logged above - check for:");
        logger.LogInformation(">>>>>>>>>> Authentication >>> 1. EXTERNAL IDP TOKEN (DSI) EXPIRY");
        logger.LogInformation(">>>>>>>>>> Authentication >>> 2. AZURE TOKEN EXPIRY"); 
        logger.LogInformation(">>>>>>>>>> Authentication >>> 3. INTERNAL/OBO TOKEN EXPIRY");
        logger.LogInformation(">>>>>>>>>> Authentication >>> ===== PROCEEDING WITH API CALL =====");
        
        logger.LogDebug(">>>>>>>>>> Authentication >>> Sending request with internal token: {Method} {Uri}", 
            request.Method, request.RequestUri);
        
        var finalResponse = await base.SendAsync(request, cancellationToken);
        
        logger.LogInformation(">>>>>>>>>> Authentication >>> Final request completed with status: {StatusCode} for {Method} {Uri}", 
            finalResponse.StatusCode, request.Method, request.RequestUri);

        if (finalResponse.StatusCode == HttpStatusCode.Unauthorized)
        {
            logger.LogError(">>>>>>>>>> Authentication >>> 401 Unauthorized after token exchange - internal token may be invalid or user may lack permissions for {Method} {Uri}", 
                request.Method, request.RequestUri);
        }
        else if (finalResponse.StatusCode == HttpStatusCode.Forbidden)
        {
            logger.LogError(">>>>>>>>>> Authentication >>> 403 Forbidden after token exchange - user may lack specific permissions for {Method} {Uri}", 
                request.Method, request.RequestUri);
        }
        
        return finalResponse;
    }

    private static bool IsTokenValid(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            var isValid = jwt.ValidTo > DateTime.UtcNow.Add(ExpiryBuffer);
            return isValid;
        }
        catch
        {
            return false;
        }
    }

    private static DateTime GetTokenExpiry(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo;
        }
        catch
        {
            return DateTime.UtcNow; // Default fallback for invalid tokens
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
