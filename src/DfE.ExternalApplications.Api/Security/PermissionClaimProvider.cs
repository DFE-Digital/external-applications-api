using DfE.CoreLibs.Security.Interfaces;
using DfE.ExternalApplications.Application.Users.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DfE.ExternalApplications.Infrastructure.Security.Authorization
{
    public class PermissionsClaimProvider(ISender sender, ILogger<PermissionsClaimProvider> logger) : ICustomClaimProvider
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

            var query = new GetAllUserPermissionsByExternalProviderIdQuery(clientId);
            var result = await sender.Send(query);

            if (result is { IsSuccess: false })
            {
                logger.LogWarning($"PermissionsClaimProvider() > Failed to return the user permissions for Azure AppId:{clientId}");
                return Array.Empty<Claim>();
            }

            return result.Value == null ? 
                Array.Empty<Claim>() : 
                result.Value.Select(p =>
                new Claim(
                    "permission",
                    $"{p.ResourceType}:{p.ResourceKey}:{p.AccessType}"
                )
            );
        }
    }
}
