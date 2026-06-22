using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Users.Queries
{
    public sealed record GetMyPermissionsQuery()
        : IRequest<Result<UserAuthorizationDto>>;

    public sealed class GetMyPermissionsQueryHandler(
        IHttpContextAccessor httpContextAccessor,
        IEaRepository<User> userRepo,
        ISender mediator)
        : IRequestHandler<GetMyPermissionsQuery, Result<UserAuthorizationDto>>
    {
        public async Task<Result<UserAuthorizationDto>> Handle(
            GetMyPermissionsQuery request,
            CancellationToken cancellationToken)
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user is null || !user.Identity?.IsAuthenticated == true)
                return Result<UserAuthorizationDto>.Forbid("Not authenticated");

            var principalId = user.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                return Result<UserAuthorizationDto>.Forbid("No user identifier");

            User? dbUser;
            if (principalId.Contains('@'))
            {
                dbUser = await (new GetUserByEmailQueryObject(principalId))
                    .Apply(userRepo.Query().AsNoTracking())
                    .FirstOrDefaultAsync(cancellationToken);
            }
            else
            {
                dbUser = await (new GetUserByExternalProviderIdQueryObject(principalId))
                    .Apply(userRepo.Query().AsNoTracking())
                .FirstOrDefaultAsync(cancellationToken);
            }

            if (dbUser is null)
                return Result<UserAuthorizationDto>.NotFound("User not found");

            // The caller is authenticated and matched to this user by token identity (email or external id).
            // Returning their own permissions is safe without a separate User:Read claim, which invited
            // contributors may not have been provisioned with historically.
            return await mediator.Send(new GetAllUserPermissionsQuery(dbUser.Id), cancellationToken);

        }
    }
}
