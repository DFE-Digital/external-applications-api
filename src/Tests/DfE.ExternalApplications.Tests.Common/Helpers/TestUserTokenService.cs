using DfE.CoreLibs.Security.Configurations;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

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
    }
}
