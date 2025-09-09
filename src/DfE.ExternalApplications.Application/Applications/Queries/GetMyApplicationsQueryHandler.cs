using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record GetMyApplicationsQuery(bool IncludeSchema = false, Guid? TemplateId = null) : IRequest<Result<IReadOnlyCollection<ApplicationDto>>>;

public sealed class GetMyApplicationsQueryHandler(
    IHttpContextAccessor httpContextAccessor,
    ISender mediator)
    : IRequestHandler<GetMyApplicationsQuery, Result<IReadOnlyCollection<ApplicationDto>>>
{
    public async Task<Result<IReadOnlyCollection<ApplicationDto>>> Handle(
        GetMyApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null || !user.Identity?.IsAuthenticated == true)
            return Result<IReadOnlyCollection<ApplicationDto>>.Forbid("Not authenticated");

        var principalId = user.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(principalId))
            principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

        if (string.IsNullOrEmpty(principalId))
            return Result<IReadOnlyCollection<ApplicationDto>>.Forbid("No user identifier");

        Result<IReadOnlyCollection<ApplicationDto>> result;
        if (principalId.Contains('@'))
        {
            result = await mediator.Send(new GetApplicationsForUserQuery(principalId, request.IncludeSchema, request.TemplateId), cancellationToken);
        }
        else
        {
            result = await mediator.Send(new GetApplicationsForUserByExternalProviderIdQuery(principalId, request.IncludeSchema, request.TemplateId), cancellationToken);
        }

        return result;
    }
}