using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record GetApplicationByReferenceQuery(string ApplicationReference, bool IncludeSchema = true)
    : IRequest<Result<ApplicationDto>>;

public sealed class GetApplicationByReferenceQueryHandler(
    IApplicationRepository applicationRepo,
    IHttpContextAccessor httpContextAccessor,
    IPermissionCheckerService permissionCheckerService)
    : IRequestHandler<GetApplicationByReferenceQuery, Result<ApplicationDto>>
{
    public async Task<Result<ApplicationDto>> Handle(
        GetApplicationByReferenceQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<ApplicationDto>.Forbid("Not authenticated");

            var dto = await new GetApplicationByReferenceDtoQueryObject(request.ApplicationReference, request.IncludeSchema)
                .Apply(applicationRepo.Query())
                .FirstOrDefaultAsync(cancellationToken);

            if (dto is null)
                return Result<ApplicationDto>.NotFound("Application not found");

            var canAccess = permissionCheckerService.HasPermission(
                ResourceType.Application, 
                dto.ApplicationId.ToString(),
                AccessType.Read);

            if (!canAccess)
                return Result<ApplicationDto>.Forbid("User does not have permission to read this application");

            var latestResponseEntity = await applicationRepo.GetLatestResponseAsync(
                new ApplicationId(dto.ApplicationId),
                cancellationToken);

            var latestResponse = latestResponseEntity is null
                ? null
                : MapLatestResponse(latestResponseEntity);

            return Result<ApplicationDto>.Success(dto with { LatestResponse = latestResponse });
        }
        catch (Exception e)
        {
            return Result<ApplicationDto>.Failure(e.Message);
        }
    }

    private static ApplicationResponseDetailsDto MapLatestResponse(Domain.Entities.ApplicationResponse response) =>
        new()
        {
            ResponseId = response.Id!.Value,
            ResponseBody = response.ResponseBody,
            CreatedOn = response.CreatedOn,
            CreatedBy = response.CreatedBy.Value,
            LastModifiedOn = response.LastModifiedOn,
            LastModifiedBy = response.LastModifiedBy?.Value
        };
}