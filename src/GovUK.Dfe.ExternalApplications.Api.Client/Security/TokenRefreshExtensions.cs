using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Security;

public static class TokenRefreshExtensions
{
    /// <summary>
    /// Forces a token refresh by clearing the stored internal token. This will cause a new token exchange on the next API request,
    /// ensuring that any changes to user permissions are reflected in the new token.
    /// </summary>
    public static void ForceTokenRefresh(this IHttpContextAccessor httpContextAccessor)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null) return;

        var tokenStore = httpContext.RequestServices.GetRequiredService<IInternalUserTokenStore>();
        tokenStore.ClearToken();
    }
} 