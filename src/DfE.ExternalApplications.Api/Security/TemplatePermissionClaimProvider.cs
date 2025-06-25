using System.Security.Claims;
using DfE.CoreLibs.Security.Interfaces;
using DfE.ExternalApplications.Application.TemplatePermissions.Queries;
using MediatR;
using Microsoft.IdentityModel.JsonWebTokens;

namespace DfE.ExternalApplications.Api.Security;
public class TemplatePermissionsClaimProvider(ISender sender, ILogger<TemplatePermissionsClaimProvider> logger) : ICustomClaimProvider
{
    public async Task<IEnumerable<Claim>> GetClaimsAsync(ClaimsPrincipal principal)
    {
        var issuer = principal.FindFirst(JwtRegisteredClaimNames.Iss)?.Value
                     ?? principal.FindFirst("iss")?.Value;
        if (string.IsNullOrEmpty(issuer) || !issuer.Contains("windows.net", StringComparison.OrdinalIgnoreCase))
            return Array.Empty<Claim>();

        var clientId = principal.FindFirst("appid")?.Value;
        if (string.IsNullOrEmpty(clientId))
        {
            logger.LogWarning("TemplatePermissionsClaimProvider() > Azure token had no appid");
            return Array.Empty<Claim>();
        }

        var query = new GetTemplatePermissionsForUserByExternalProviderIdQuery(clientId);
        var result = await sender.Send(query);

        if (result is { IsSuccess: false })
        {
            logger.LogWarning($"TemplatePermissionsClaimProvider() > Failed to return the template permissions for Azure AppId:{clientId}");
            return Array.Empty<Claim>();
        }

        return result.Value == null
            ? Array.Empty<Claim>()
            : result.Value.Select(p => new Claim(
                "permission",
                $"Template:{p.TemplateId}:{p.AccessType}")
            );
    }
}