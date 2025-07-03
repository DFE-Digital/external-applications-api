using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;

namespace GovUK.Dfe.ExternalApplications.Api.Client.Security
{
    public class HttpContextInternalUserTokenStore(IHttpContextAccessor httpContextAccessor) : IInternalUserTokenStore
    {
        private const string TokenKey = "__InternalUserToken";

        public string? GetToken()
        {
            var ctx = httpContextAccessor.HttpContext;
            if (ctx == null)
            {
                return null;
            }

            if (ctx.Items.TryGetValue(TokenKey, out var tokenObj) && tokenObj is string token)
            {
                var handler = new JwtSecurityTokenHandler();
                var jwt = handler.ReadJwtToken(token);
                if (jwt.ValidTo <= DateTime.UtcNow.AddMinutes(1))
                {
                    ctx.Items.Remove(TokenKey);
                    return null;
                }

                return token;
            }

            return null;
        }

        public void SetToken(string token)
        {
            var ctx = httpContextAccessor.HttpContext;
            if (ctx != null)
            {
                ctx.Items[TokenKey] = token;
            }
        }

        public void ClearToken()
        {
            var ctx = httpContextAccessor.HttpContext;
            ctx?.Items.Remove(TokenKey);
        }
    }
}