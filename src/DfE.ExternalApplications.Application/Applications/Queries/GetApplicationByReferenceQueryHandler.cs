using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record GetApplicationByReferenceQuery(string ApplicationReference)
    : IRequest<Result<ApplicationDto>>;

public sealed class GetApplicationByReferenceQueryHandler(
    IEaRepository<Domain.Entities.Application> applicationRepo,
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

            var application = await (new GetApplicationByReferenceQueryObject(request.ApplicationReference))
                .Apply(applicationRepo.Query().AsNoTracking())
                .FirstOrDefaultAsync(cancellationToken);

            if (application is null)
                return Result<ApplicationDto>.NotFound("Application not found");

            var canAccess = permissionCheckerService.HasPermission(
                ResourceType.Application, 
                application.Id!.Value.ToString(), 
                AccessType.Read);

            if (!canAccess)
                return Result<ApplicationDto>.Forbid("User does not have permission to read this application");

            // Get the latest response
            var latestResponse = application.GetLatestResponse();

            var responseDto = latestResponse != null 
                ? new ApplicationResponseDetailsDto
                {
                    ResponseId = latestResponse.Id!.Value,
                    ResponseBody = latestResponse.ResponseBody,
                    CreatedOn = latestResponse.CreatedOn,
                    CreatedBy = latestResponse.CreatedBy.Value,
                    LastModifiedOn = latestResponse.LastModifiedOn,
                    LastModifiedBy = latestResponse.LastModifiedBy?.Value
                }
                : null;

            var result = new ApplicationDto
            {
                ApplicationId = application.Id!.Value,
                ApplicationReference = application.ApplicationReference,
                TemplateVersionId = application.TemplateVersionId.Value,
                TemplateName = application.TemplateVersion?.Template?.Name ?? string.Empty,
                Status = application.Status,
                DateCreated = application.CreatedOn,
                DateSubmitted = application.Status == ApplicationStatus.Submitted ? application.LastModifiedOn : null,
                LatestResponse = responseDto,
                TemplateSchema = application.TemplateVersion != null ? new TemplateSchemaDto
                {
                    TemplateId = application.TemplateVersion.Template?.Id?.Value ?? Guid.Empty,
                    TemplateVersionId = application.TemplateVersion.Id!.Value,
                    VersionNumber = application.TemplateVersion.VersionNumber,
                    JsonSchema = application.TemplateVersion.JsonSchema
                } : null,
                CreatedBy = application.CreatedByUser == null ? null : new UserDto
                {
                    UserId = application.CreatedByUser.Id!.Value,
                    Name = application.CreatedByUser.Name,
                    Email = application.CreatedByUser.Email
                }
            };

            return Result<ApplicationDto>.Success(result);
        }
        catch (Exception e)
        {
            return Result<ApplicationDto>.Failure(e.Message);
        }
    }
} 