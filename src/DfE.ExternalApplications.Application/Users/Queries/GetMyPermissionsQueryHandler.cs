using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Security.Interfaces;
using MediatR;

namespace DfE.ExternalApplications.Application.Users.Queries
{
    public sealed record GetMyPermissionsQuery()
        : IRequest<Result<IReadOnlyCollection<UserPermissionDto>>>;

    public sealed class GetMyPermissionsQueryHandler(
        ICurrentUser currentUser,
        ISender mediator)
        : IRequestHandler<GetMyPermissionsQuery, Result<IReadOnlyCollection<UserPermissionDto>>>
    {
        public async Task<Result<IReadOnlyCollection<UserPermissionDto>>> Handle(
            GetMyPermissionsQuery request,
            CancellationToken cancellationToken)
        {
            if (!currentUser.IsAuthenticated)
                return Result<IReadOnlyCollection<UserPermissionDto>>.Failure("Not authenticated");

            var principalId = currentUser.Email
                              ?? currentUser.GetClaimValue("appid")
                              ?? currentUser.GetClaimValue("azp");

            if (string.IsNullOrEmpty(principalId))
                return Result<IReadOnlyCollection<UserPermissionDto>>.Failure("No user identifier");

            Result<IReadOnlyCollection<UserPermissionDto>> result;
            if (principalId.Contains('@'))
            {
                result = await mediator.Send(new GetAllUserPermissionsQuery(principalId), cancellationToken);
            }
            else
            {
                result = await mediator.Send(new GetAllUserPermissionsByExternalProviderIdQuery(principalId), cancellationToken);
            }

            return result;
        }
    }
}
