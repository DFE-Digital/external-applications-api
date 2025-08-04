using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Security.Interfaces;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;

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
                        ?? throw new SecurityTokenException("ExchangeTokenQueryHandler > Missing email");

            var dbUser = await (new GetUserByEmailQueryObject(email))
                .Apply(userRepo.Query().AsNoTracking())
                .FirstOrDefaultAsync(cancellationToken: ct);

            if (dbUser is null)
                throw new SecurityTokenException($"ExchangeTokenQueryHandler > User not found for email {email}");

            if (dbUser.Role is null)
                throw new SecurityTokenException($"ExchangeTokenQueryHandler > User {email} has no role assigned");

            var httpCtx = httpCtxAcc.HttpContext!;
            var azureAuth = await httpCtx.AuthenticateAsync("AzureEntra");
            var svcRoles = azureAuth.Succeeded
                ? azureAuth.Principal.Claims.Where(c => c.Type == ClaimTypes.Role || c.Type == "roles")
                : Enumerable.Empty<Claim>();

            // Create new identity with only specific claims from external user
            var identity = new ClaimsIdentity();
            var allowedClaimTypes = new[]
            {
                ClaimTypes.NameIdentifier,
                ClaimTypes.Email,
                ClaimTypes.GivenName,
                ClaimTypes.Surname,
                "organisation"
            };

            foreach (var claim in externalUser.Claims)
            {
                if (allowedClaimTypes.Contains(claim.Type))
                {
                    identity.AddClaim(claim);
                }
            }
            identity.AddClaim(new Claim(ClaimTypes.Role, dbUser.Role.Name));
            
            identity.AddClaims(svcRoles);

            var mergedUser = new ClaimsPrincipal(identity);

            var internalToken = await tokenSvc.GetUserTokenModelAsync(mergedUser);
            return new ExchangeTokenDto
            {
                AccessToken = internalToken.AccessToken,
                TokenType = "Bearer",
                ExpiresIn = internalToken.ExpiresIn,
                RefreshToken = internalToken.RefreshToken,
                Scope = internalToken.Scope,
                IdToken = internalToken.IdToken,
                RefreshExpiresIn = internalToken.RefreshExpiresIn
            };
        }
    }
}
