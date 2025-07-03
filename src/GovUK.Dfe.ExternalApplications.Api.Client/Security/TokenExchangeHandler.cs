using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;
using GovUK.Dfe.ExternalApplications.Api.Client.Extensions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Security
{
    /// <summary>
    /// Exchanges an external identity provider token for an internal user token
    /// on first use and stores it for later requests.
    /// </summary>
    public class TokenExchangeHandler(
        IHttpContextAccessor httpContextAccessor,
        IInternalUserTokenStore tokenStore,
        ITokensClient tokensClient)
        : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(tokenStore.GetToken()))
            {
                var ctx = httpContextAccessor.HttpContext;
                if (ctx != null)
                {
                    var idToken = await ctx.GetTokenAsync("id_token");
                    if (!string.IsNullOrEmpty(idToken))
                    {
                        await tokensClient.ExchangeAndStoreAsync(idToken, tokenStore, cancellationToken);
                    }
                }
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }
}