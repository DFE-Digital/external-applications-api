using GovUK.Dfe.CoreLibs.Caching.Helpers;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using DfE.ExternalApplications.Application.Common;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Tenancy;
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
        ICacheService<IRedisCacheType> cacheService,
        ITenantContextAccessor tenantContextAccessor
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

            var baseCacheKey = $"UserClaims_{CacheKeyHelper.GenerateHashedCacheKey(userEmail.ToLower())}";
            var cacheKey = TenantCacheKeyHelper.CreateTenantScopedKey(tenantContextAccessor, baseCacheKey);
            var methodName = nameof(UserPermissionClaimProvider);

            // Cache as list of strings (serializable) instead of Claim objects (not serializable)
            var permissionValues = await cacheService.GetOrAddAsync<List<string>>(
                cacheKey,
                async () =>
                {
                    var dbUser = await (new GetUserByEmailQueryObject(userEmail))
                        .Apply(userRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync();

                    if (dbUser is null)
                        return new List<string>();

                    if (dbUser.Role is null)
                        return new List<string>();

                    var userWithPerms = await new GetUserWithAllPermissionsByUserIdQueryObject(dbUser.Id!)
                        .Apply(userRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync();
                    var userPerms = userWithPerms?.Permissions;

                    var templateWithPerms = await new GetUserWithAllTemplatePermissionsQueryObject(userEmail)
                        .Apply(userRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync();
                    var templatePerms = templateWithPerms?.TemplatePermissions;

                    var permValues = (from p in userPerms ?? [] select $"{p.ResourceType}:{p.ResourceKey}:{p.AccessType}").ToList();
                    permValues.AddRange(from tp in templatePerms ?? [] select $"Template:{tp.TemplateId.Value}:{tp.AccessType.ToString()}");

                    return permValues;

                },
                methodName);

            // Convert cached string values back to Claim objects
            return permissionValues.Select(v => new Claim("permission", v));
        }
    }
}
