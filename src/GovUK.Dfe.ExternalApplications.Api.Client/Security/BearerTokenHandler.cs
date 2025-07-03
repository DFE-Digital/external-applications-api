using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Security
{
    [ExcludeFromCodeCoverage]
    public class BearerTokenHandler(
        ITokenAcquisitionService tokenAcquisitionService,
        IInternalUserTokenStore tokenStore) : DelegatingHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var internalToken = tokenStore.GetToken();
            if (!string.IsNullOrEmpty(internalToken) && !IsNearExpiry(internalToken))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", internalToken);
            }
            else
            {
                if (!string.IsNullOrEmpty(internalToken))
                {
                    tokenStore.ClearToken();
                }

                var token = await tokenAcquisitionService.GetTokenAsync();
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }

        private static bool IsNearExpiry(string token)
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.ValidTo <= DateTime.UtcNow.AddMinutes(1);
        }
    }
}
