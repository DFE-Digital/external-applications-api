using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Security;

public class CachedInternalUserTokenStore(
    IHttpContextAccessor httpContextAccessor,
    IDistributedCache distributedCache,
    ILogger<CachedInternalUserTokenStore> logger)
    : IInternalUserTokenStore
{
    private const string TokenKey = "__InternalUserToken";
    private const string CacheKeyPrefix = "InternalToken:";
    private static readonly TimeSpan CacheBuffer = TimeSpan.FromMinutes(5);

    public string? GetToken()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx == null) return null;

        // First check HttpContext.Items for this request
        if (ctx.Items.TryGetValue(TokenKey, out var tokenObj) && tokenObj is string requestToken)
        {
            if (IsTokenValid(requestToken))
            {
                return requestToken;
            }
            ctx.Items.Remove(TokenKey);
        }

        // Then check distributed cache
        var userId = GetUserIdFromContext(ctx);
        if (userId != null)
        {
            var cacheKey = $"{CacheKeyPrefix}{userId}";
            var cachedTokenJson = distributedCache.GetString(cacheKey);
            
            if (!string.IsNullOrEmpty(cachedTokenJson))
            {
                try
                {
                    var cachedToken = JsonSerializer.Deserialize<CachedTokenData>(cachedTokenJson);
                    if (cachedToken != null && IsTokenValid(cachedToken.Token))
                    {
                        // Store in HttpContext.Items for subsequent requests in this request
                        ctx.Items[TokenKey] = cachedToken.Token;
                        return cachedToken.Token;
                    }
                    else
                    {
                        // Remove expired token from cache
                        distributedCache.Remove(cacheKey);
                    }
                }
                catch (JsonException ex)
                {
                    logger.LogWarning(ex, "Failed to deserialize cached token for user {UserId}", userId);
                    distributedCache.Remove(cacheKey);
                }
            }
        }

        return null;
    }

    public void SetToken(string token)
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx == null) return;

        ctx.Items[TokenKey] = token;

        // Store in distributed cache for future requests
        var userId = GetUserIdFromContext(ctx);
        if (userId != null)
        {
            var cacheKey = $"{CacheKeyPrefix}{userId}";
            var tokenData = new CachedTokenData(token, GetTokenExpiry(token));
            var tokenJson = JsonSerializer.Serialize(tokenData);
            
            var cacheOptions = new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = tokenData.ExpiresAt.Subtract(CacheBuffer)
            };
            
            distributedCache.SetString(cacheKey, tokenJson, cacheOptions);
            logger.LogDebug("Internal token cached for user {UserId} until {ExpiryTime}", userId, cacheOptions.AbsoluteExpiration);
        }
    }

    public void ClearToken()
    {
        var ctx = httpContextAccessor.HttpContext;
        if (ctx == null) return;

        // Clear from HttpContext.Items
        ctx.Items.Remove(TokenKey);

        // Clear from distributed cache
        var userId = GetUserIdFromContext(ctx);
        if (userId != null)
        {
            var cacheKey = $"{CacheKeyPrefix}{userId}";
            distributedCache.Remove(cacheKey);
            logger.LogDebug("Cleared internal token cache for user {UserId}", userId);
        }
    }

    private static bool IsTokenValid(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo > DateTime.UtcNow.AddMinutes(1);
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
            return DateTime.UtcNow.AddHours(1); // Default fallback
        }
    }

    private static string? GetUserIdFromContext(HttpContext context)
    {
        // Try to get user identifier from claims
        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            // Try different claim types that might identify the user
            return user.FindFirst("appid")?.Value ??
                   user.FindFirst("azp")?.Value ??
                   user.FindFirst(ClaimTypes.Email)?.Value ??
                   user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        }
        return null;
    }

    private record CachedTokenData(string Token, DateTime ExpiresAt);
} 