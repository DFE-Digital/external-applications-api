using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Users.Queries
{
    public sealed record GetMyPermissionsQuery()
        : IRequest<Result<IReadOnlyCollection<UserPermissionDto>>>;

    public sealed class GetMyPermissionsQueryHandler(
        IHttpContextAccessor httpContext,
        ISender mediator)
        : IRequestHandler<GetMyPermissionsQuery, Result<IReadOnlyCollection<UserPermissionDto>>>
    {
        public async Task<Result<IReadOnlyCollection<UserPermissionDto>>> Handle(
            GetMyPermissionsQuery request,
            CancellationToken cancellationToken)
        {
            var user = httpContext.HttpContext?.User;
            if (user is null || !user.Identity?.IsAuthenticated == true)
                return Result<IReadOnlyCollection<UserPermissionDto>>.Failure("Not authenticated");

            var principalId = user.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

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
