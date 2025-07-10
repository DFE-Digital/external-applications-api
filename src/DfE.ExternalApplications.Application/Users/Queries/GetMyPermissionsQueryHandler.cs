using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
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
        IPermissionCheckerService permissionCheckerService,
        ISender mediator)
        : IRequestHandler<GetMyPermissionsQuery, Result<UserAuthorizationDto>>
    {
        public async Task<Result<UserAuthorizationDto>> Handle(
            GetMyPermissionsQuery request,
            CancellationToken cancellationToken)
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user is null || !user.Identity?.IsAuthenticated == true)
                return Result<UserAuthorizationDto>.Failure("Not authenticated");

            var principalId = user.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                return Result<UserAuthorizationDto>.Failure("No user identifier");

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
                return Result<UserAuthorizationDto>.Failure("User not found");

            var canAccess = permissionCheckerService.HasPermission(ResourceType.User, dbUser.Id!.Value.ToString(), AccessType.Read);
            var canAccessByEmail = permissionCheckerService.HasPermission(ResourceType.User, dbUser.Email, AccessType.Read);
            var canAccessByExtId = permissionCheckerService.HasPermission(ResourceType.User, dbUser?.ExternalProviderId ?? "", AccessType.Read);

            if (!canAccess && !canAccessByEmail && !canAccessByExtId)
                return Result<UserAuthorizationDto>.Failure("User does not have permission to view permissions.");

            return await mediator.Send(new GetAllUserPermissionsQuery(dbUser.Id), cancellationToken);

        }
    }
}
