using GovUK.Dfe.CoreLibs.Caching.Helpers;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace DfE.ExternalApplications.Api.Security
{
    public class UserPermissionClaimProvider(
        ISender sender,
        ILogger<UserPermissionClaimProvider> logger,
        IEaRepository<User> userRepo,
        ICacheService<IMemoryCacheType> cacheService
        ) : ICustomClaimProvider
    {
        public async Task<IEnumerable<Claim>> GetClaimsAsync(ClaimsPrincipal principal)
        {
            var issuer = principal.FindFirst(JwtRegisteredClaimNames.Iss)?.Value
                         ?? principal.FindFirst("iss")?.Value;
            if (string.IsNullOrEmpty(issuer) || issuer.Contains("windows.net", StringComparison.OrdinalIgnoreCase))
            {
                return Array.Empty<Claim>();
            }
            var userEmail = principal.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(userEmail))
            {
                logger.LogWarning("UserPermissionsClaimProvider() > User Email Address not found.");
                return Array.Empty<Claim>();
            }

            var cacheKey = $"UserClaims_{CacheKeyHelper.GenerateHashedCacheKey(userEmail.ToLower())}";
            var methodName = nameof(UserPermissionClaimProvider);

            return await cacheService.GetOrAddAsync<IEnumerable<Claim>>(
                cacheKey,
                async () =>
                {
                    var dbUser = await (new GetUserByEmailQueryObject(userEmail))
                        .Apply(userRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync();

                    if (dbUser is null)
                        return Array.Empty<Claim>();

                    if (dbUser.Role is null)
                        return Array.Empty<Claim>();

                    var userWithPerms = await new GetUserWithAllPermissionsByUserIdQueryObject(dbUser.Id!)
                        .Apply(userRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync();
                    var userPerms = userWithPerms?.Permissions;

                    var templateWithPerms = await new GetUserWithAllTemplatePermissionsQueryObject(userEmail)
                        .Apply(userRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync();
                    var templatePerms = templateWithPerms?.TemplatePermissions;

                    var claims = (from p in userPerms ?? [] select new Claim("permission", $"{p.ResourceType}:{p.ResourceKey}:{p.AccessType}")).ToList();
                    claims.AddRange(from tp in templatePerms ?? [] select new Claim("permission", $"Template:{tp.TemplateId.Value}:{tp.AccessType.ToString()}"));

                    return claims;

                },
                methodName);
        }
    }
}
