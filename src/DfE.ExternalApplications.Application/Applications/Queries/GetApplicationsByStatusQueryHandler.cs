using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record GetApplicationsByStatusQuery(ApplicationStatus? Status)
    : IRequest<Result<IReadOnlyCollection<ApplicationDto>>>;

public sealed class GetApplicationsByStatusQueryHandler(
    IEaRepository<Domain.Entities.Application> appRepo,
    IPermissionCheckerService permissionCheckerService,
    IHttpContextAccessor httpContextAccessor
    ) : IRequestHandler<GetApplicationsByStatusQuery, Result<IReadOnlyCollection<ApplicationDto>>>
{
    public async Task<Result<IReadOnlyCollection<ApplicationDto>>> Handle(
        GetApplicationsByStatusQuery request,
        CancellationToken cancellationToken)
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
            return Result<IReadOnlyCollection<ApplicationDto>>.Forbid("Not authenticated");

        var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

        if (string.IsNullOrEmpty(principalId))
            principalId = user.FindFirstValue(ClaimTypes.Email);

        if (string.IsNullOrEmpty(principalId))
            return Result<IReadOnlyCollection<ApplicationDto>>.Forbid("No user identifier");

        if (!permissionCheckerService.IsAdmin() && !permissionCheckerService.IsGlobalApplicationReader())
            return Result<IReadOnlyCollection<ApplicationDto>>.Forbid("User does not have permission");

        GetApplicationsByStatusQueryObject queryObject = new(request.Status);
        IQueryable<Domain.Entities.Application> objQuery = queryObject.Apply(appRepo.Query().AsNoTracking());
        List<Domain.Entities.Application> apps = await objQuery.ToListAsync(cancellationToken);
        var dtoList = apps.Select(a => new ApplicationDto
        {
            ApplicationId = a.Id!.Value,
            ApplicationReference = a.ApplicationReference,
            TemplateVersionId = a.TemplateVersionId.Value,
            DateCreated = a.CreatedOn,
            DateSubmitted = a.Status == ApplicationStatus.Submitted ? a.LastModifiedOn : null,
            Status = a.Status
        }).ToList().AsReadOnly();

        return Result<IReadOnlyCollection<ApplicationDto>>.Success(dtoList);
    }
}
