using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;
using GovUK.Dfe.ExternalApplications.Api.Client.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.IdentityModel.Tokens.Jwt;
using System.Net;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Security;

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

        // No valid internal token, need to exchange
        logger.LogDebug("No valid internal token, starting exchange process");

        try
        {
            // Get DSI token from authentication context
            var externalIdpToken = await httpContext?.GetTokenAsync("id_token");
            if (string.IsNullOrEmpty(externalIdpToken) || !IsTokenValid(externalIdpToken))
            {
                logger.LogWarning("No valid DSI token found");
                await ForceSignOut(httpContext);
                return UnauthorizedResponse(request);
            }

            // Azure token (for authorization to call exchange endpoint)
            logger.LogDebug("Getting Azure token for exchange endpoint authorization");
            var azureToken = await tokenAcquisitionService.GetTokenAsync();

            // Call exchange endpoint with DSI token in body
            logger.LogDebug("Calling exchange endpoint with External IDP token");
            var exchangeResult = await tokensClient.ExchangeAndStoreAsync(externalIdpToken, tokenStore, cancellationToken);
            
            if (string.IsNullOrEmpty(exchangeResult.AccessToken))
            {
                logger.LogWarning("Token exchange returned empty internal token");
                await ForceSignOut(httpContext);
                return UnauthorizedResponse(request);
            }

            logger.LogDebug("Token exchange successful, internal token cached");

            // Use the new internal token for the current request
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", exchangeResult.AccessToken);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Token exchange process failed");
            await ForceSignOut(httpContext);
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
        return new HttpResponseMessage(HttpStatusCode.Unauthorized)
        {
            RequestMessage = request,
            ReasonPhrase = "Token exchange failed - user needs to re-authenticate"
        };
    }

    private static async Task ForceSignOut(HttpContext? ctx)
    {
        if (ctx == null) return;

        try
        {
            await ctx.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            await ctx.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme);
        }
        catch
        {
            // Ignore sign-out errors
        }
    }
}
