using DfE.CoreLibs.Security.Interfaces;
using DfE.ExternalApplications.Application.Common.Models;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;

namespace DfE.ExternalApplications.Application.Users.Queries
{
    public record ExchangeTokenQuery(string SubjectToken) : IRequest<ExchangeTokenDto>;

    public class ExchangeTokenQueryHandler(
        IExternalIdentityValidator externalValidator,
        IEaRepository<User> userRepo,
        IUserTokenService tokenSvc,
        IHttpContextAccessor httpCtxAcc)
        : IRequestHandler<ExchangeTokenQuery, ExchangeTokenDto>
    {
        public async Task<ExchangeTokenDto> Handle(ExchangeTokenQuery req, CancellationToken ct)
        {
            var externalUser = await externalValidator
                .ValidateIdTokenAsync(req.SubjectToken, ct);

            var email = externalUser.FindFirst(ClaimTypes.Email)?.Value
                        ?? throw new SecurityTokenException("Missing email");

            var httpCtx = httpCtxAcc.HttpContext!;
            var azureAuth = await httpCtx.AuthenticateAsync("AzureEntra");
            var svcRoles = azureAuth.Succeeded
                ? azureAuth.Principal.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
                : Enumerable.Empty<Claim>();

            var identity = new ClaimsIdentity(externalUser.Identity!);
            identity.AddClaims(svcRoles);

            var userWithPerms = await new GetUserWithAllPermissionsQueryObject(email)
                .Apply(userRepo.Query().AsNoTracking())
                .FirstOrDefaultAsync(ct);
            var userPerms = userWithPerms?.Permissions;

            var templateWithPerms = await new GetUserWithAllTemplatePermissionsQueryObject(email)
                .Apply(userRepo.Query().AsNoTracking())
                .FirstOrDefaultAsync(ct);
            var templatePerms = templateWithPerms?.TemplatePermissions;

            foreach (var p in userPerms ?? [])
                identity.AddClaim(new Claim(
                    "permission",
                    $"{p.ResourceType}:{p.ResourceKey}:{p.AccessType}"));
            foreach (var tp in templatePerms ?? [])
                identity.AddClaim(new Claim(
                    "permission",
                    $"Template:{tp.TemplateId.Value}:{tp.AccessType.ToString()}"));

            var mergedUser = new ClaimsPrincipal(identity);

            var internalToken = await tokenSvc.GetUserTokenAsync(mergedUser);
            return new ExchangeTokenDto(internalToken);
        }
    }
}
