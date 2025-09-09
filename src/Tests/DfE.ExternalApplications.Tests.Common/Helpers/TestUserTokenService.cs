using GovUK.Dfe.CoreLibs.Security.Configurations;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using GovUK.Dfe.CoreLibs.Security.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace DfE.ExternalApplications.Tests.Common.Helpers
{
    public class TestUserTokenService(IOptions<TokenSettings> options) : IUserTokenService
    {
        private readonly TokenSettings _settings = options.Value;

        public Task<string> GetUserTokenAsync(ClaimsPrincipal principal)
        {
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: principal.Claims,
                expires: DateTime.UtcNow.AddMinutes(_settings.TokenLifetimeMinutes),
                signingCredentials: creds);
            return Task.FromResult(handler.WriteToken(jwt));
        }

        public Task<Token> GetUserTokenModelAsync(ClaimsPrincipal principal)
        {
            var expiryTime = DateTime.UtcNow.AddMinutes(_settings.TokenLifetimeMinutes);
            var handler = new JwtSecurityTokenHandler();
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(
                issuer: _settings.Issuer,
                audience: _settings.Audience,
                claims: principal.Claims,
                expires: expiryTime,
                signingCredentials: creds);
            return Task.FromResult(new Token
            {
                AccessToken = handler.WriteToken(jwt),
            });
        }
    }
}
