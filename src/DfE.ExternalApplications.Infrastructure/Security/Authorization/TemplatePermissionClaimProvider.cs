using DfE.CoreLibs.Security.Interfaces;
using DfE.ExternalApplications.Application.Users.Queries;
using MediatR;
using Microsoft.Extensions.Logging;
using System.Security.Claims;

namespace DfE.ExternalApplications.Infrastructure.Security.Authorization
{
    public class TemplatePermissionsClaimProvider(ISender sender, ILogger<TemplatePermissionsClaimProvider> logger) : ICustomClaimProvider
    {
        public async Task<IEnumerable<Claim>> GetClaimsAsync(ClaimsPrincipal principal)
        {
            var email = principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return Array.Empty<Claim>();

            var query = new GetAllUserTemplatePermissionsQuery(email);
            var result = await sender.Send(query);

            if (result is { IsSuccess: false })
            {
                logger.LogWarning($"TemplatePermissionsClaimProvider() > Failed to return the template permissions for user: {email}");
                return Array.Empty<Claim>();
            }

            return result.Value == null ? 
                Array.Empty<Claim>() : 
                result.Value.Select(p =>
                new Claim(
                    "permission",
                    $"Template:{p.TemplateId}:{p.AccessType.ToString()}"
                )
            );
        }
    }
}
