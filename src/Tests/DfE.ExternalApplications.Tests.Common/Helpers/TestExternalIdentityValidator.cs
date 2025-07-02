using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using DfE.CoreLibs.Security.Interfaces;
using Microsoft.IdentityModel.Tokens;

namespace DfE.ExternalApplications.Tests.Common.Helpers
{
    public class TestExternalIdentityValidator : IExternalIdentityValidator
    {
        private const string Secret = "9b4824fc-3360-4040-8781-75c2db3e1813";
        public Task<ClaimsPrincipal> ValidateIdTokenAsync(string token, CancellationToken cancellationToken)
        {
            var handler = new JwtSecurityTokenHandler();
            var parameters = new TokenValidationParameters
            {
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
                ValidateIssuerSigningKey = true,
            };

            var principal = handler.ValidateToken(token, parameters, out _);
            return Task.FromResult(principal);
        }

        public static string CreateToken(string email)
        {
            var claims = new[]
            {
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
            };
            var handler = new JwtSecurityTokenHandler();
            var creds = new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Secret)),
                SecurityAlgorithms.HmacSha256);
            var jwt = new JwtSecurityToken(claims: claims, signingCredentials: creds);
            return handler.WriteToken(jwt);
        }
    }
}
