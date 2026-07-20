using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace GovUK.Dfe.FlexForms.Application.Applications.Queries;

public sealed record GetMyApplicationsQuery(
    bool IncludeSchema = false,
    Guid? TemplateId = null,
    int? PageNumber = null,
    int? PageSize = null,
    ApplicationListingSearchCriteria? Search = null)
    : IRequest<Result<PagedResult<ApplicationDto>>>;

public sealed class GetMyApplicationsQueryHandler(
    IHttpContextAccessor httpContextAccessor,
    ISender mediator)
    : IRequestHandler<GetMyApplicationsQuery, Result<PagedResult<ApplicationDto>>>
{
    public async Task<Result<PagedResult<ApplicationDto>>> Handle(
        GetMyApplicationsQuery request,
        CancellationToken cancellationToken)
    {
        var user = httpContextAccessor.HttpContext?.User;
        if (user is null || !user.Identity?.IsAuthenticated == true)
            return Result<PagedResult<ApplicationDto>>.Forbid("Not authenticated");

        var principalId = user.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(principalId))
            principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

        if (string.IsNullOrEmpty(principalId))
            return Result<PagedResult<ApplicationDto>>.Forbid("No user identifier");

        if (principalId.Contains('@'))
        {
            return await mediator.Send(
                new GetApplicationsForUserQuery(principalId, request.IncludeSchema, request.TemplateId, request.PageNumber, request.PageSize, request.Search),
                cancellationToken);
        }
        else
        {
            return await mediator.Send(
                new GetApplicationsForUserByExternalProviderIdQuery(principalId, request.IncludeSchema, request.TemplateId, request.PageNumber, request.PageSize, request.Search),
                cancellationToken);
        }
    }
}
