using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;
using Microsoft.AspNetCore.Http;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record GetMyApplicationsQuery() : IRequest<Result<IReadOnlyCollection<ApplicationDto>>>;

public sealed class GetMyApplicationsQueryHandler(
    IHttpContextAccessor httpContext,
    ISender mediator)
    : IRequestHandler<GetMyApplicationsQuery, Result<IReadOnlyCollection<ApplicationDto>>>
{
    public async Task<Result<IReadOnlyCollection<ApplicationDto>>> Handle(
        GetMyApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var user = httpContext.HttpContext?.User;
        if (user is null || !user.Identity?.IsAuthenticated == true)
            return Result<IReadOnlyCollection<ApplicationDto>>.Failure("Not authenticated");

        var principalId = user.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(principalId))
            principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

        if (string.IsNullOrEmpty(principalId))
            return Result<IReadOnlyCollection<ApplicationDto>>.Failure("No user identifier");

        Result<IReadOnlyCollection<ApplicationDto>> result;
        if (principalId.Contains('@'))
        {
            result = await mediator.Send(new GetApplicationsForUserQuery(principalId), cancellationToken);
        }
        else
        {
            result = await mediator.Send(new GetApplicationsForUserByExternalProviderIdQuery(principalId), cancellationToken);
        }

        return result;
    }
}