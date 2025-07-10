using System.Security.Claims;
using DfE.CoreLibs.Security.Interfaces;
using DfE.ExternalApplications.Application.Users.Queries;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DfE.ExternalApplications.Api.Security
{
    public class PermissionsClaimProvider(
        ISender sender, 
        ILogger<PermissionsClaimProvider> logger,
        IEaRepository<User> userRepo
        ) : ICustomClaimProvider
    {
        public async Task<IEnumerable<Claim>> GetClaimsAsync(ClaimsPrincipal principal)
        {
            var issuer = principal.FindFirst(JwtRegisteredClaimNames.Iss)?.Value
                         ?? principal.FindFirst("iss")?.Value;
            if (string.IsNullOrEmpty(issuer) ||
                !issuer.Contains("windows.net", StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<Claim>();
            }

            var clientId = principal.FindFirst("appid")?.Value;

            if (string.IsNullOrEmpty(clientId))
            {
                logger.LogWarning("PermissionsClaimProvider() > Azure token had no appid");
                return Array.Empty<Claim>();
            }

            var dbUser = await (new GetUserByExternalProviderIdQueryObject(clientId))
                    .Apply(userRepo.Query().AsNoTracking())
                    .FirstOrDefaultAsync();

            if (dbUser is null)
            {
                logger.LogWarning("PermissionsClaimProvider() > Service User not found.");
                return Array.Empty<Claim>();
            }

            if (dbUser.Role is null)
            {
                logger.LogWarning($"PermissionsClaimProvider() > Service User {dbUser.Id} has no role assigned");
                return Array.Empty<Claim>();
            }

            var query = new GetAllUserPermissionsQuery(dbUser.Id!);
            var result = await sender.Send(query);

            if (result is { IsSuccess: false })
            {
                logger.LogWarning($"PermissionsClaimProvider() > Failed to return the user permissions for Azure AppId:{clientId}");
                return Array.Empty<Claim>();
            }

            var claims = new List<Claim> { new(ClaimTypes.Role, dbUser.Role.Name) };

            if(result.Value is not null)
            {
                claims.AddRange(result.Value.Select(p =>
                    new Claim(
                        "permission",
                        $"{p.ResourceType}:{p.ResourceKey}:{p.AccessType}"
                    )
                ));
            }

            return claims;
        }
    }
}
