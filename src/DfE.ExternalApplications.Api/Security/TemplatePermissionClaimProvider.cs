using GovUK.Dfe.CoreLibs.Security.Interfaces;
using DfE.ExternalApplications.Application.TemplatePermissions.Queries;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace DfE.ExternalApplications.Api.Security;
public class TemplatePermissionsClaimProvider(
    ISender sender,
    ILogger<TemplatePermissionsClaimProvider> logger,
    IEaRepository<User> userRepo) : ICustomClaimProvider
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

        var dbUser = await (new GetUserByExternalProviderIdQueryObject(clientId))
                .Apply(userRepo.Query().AsNoTracking())
                .FirstOrDefaultAsync();

        if (dbUser is null)
            return Array.Empty<Claim>();

        var query = new GetTemplatePermissionsForUserByUserIdQuery(dbUser.Id!);
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