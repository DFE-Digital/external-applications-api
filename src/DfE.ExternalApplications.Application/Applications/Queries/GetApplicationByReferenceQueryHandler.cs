using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record GetApplicationByReferenceQuery(string ApplicationReference, bool IncludeSchema = true)
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

            return Result<ApplicationDto>.Success(dto);
        }
        catch (Exception e)
        {
            return Result<ApplicationDto>.Failure(e.Message);
        }
    }
}
