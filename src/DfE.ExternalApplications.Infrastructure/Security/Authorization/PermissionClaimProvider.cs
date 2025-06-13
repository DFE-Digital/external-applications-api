using DfE.CoreLibs.Security.Interfaces;
using DfE.ExternalApplications.Application.Users.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DfE.ExternalApplications.Infrastructure.Security.Authorization
{
    public class PermissionsClaimProvider(ISender sender, ILogger<PermissionsClaimProvider> logger) : ICustomClaimProvider
    {
        public async Task<IEnumerable<Claim>> GetClaimsAsync(ClaimsPrincipal principal)
        {
            var email = principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return Array.Empty<Claim>();

            var query = new GetAllUserPermissionsQuery(email);
            var result = await sender.Send(query);

            if (result is { IsSuccess: false })
            {
                logger.LogWarning($"PermissionsClaimProvider() > Failed to return the user permissions for {email}");
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
