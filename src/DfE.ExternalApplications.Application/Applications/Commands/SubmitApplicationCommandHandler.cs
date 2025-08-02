using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.Commands;

public sealed record SubmitApplicationCommand(Guid ApplicationId) : IRequest<Result<ApplicationDto>>;

public sealed class SubmitApplicationCommandHandler(
    IEaRepository<Domain.Entities.Application> applicationRepo,
    IEaRepository<User> userRepo,
    IHttpContextAccessor httpContextAccessor,
    IPermissionCheckerService permissionCheckerService,
    IUnitOfWork unitOfWork) : IRequestHandler<SubmitApplicationCommand, Result<ApplicationDto>>
{
    public async Task<Result<ApplicationDto>> Handle(
        SubmitApplicationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<ApplicationDto>.Forbid("Not authenticated");

            var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(principalId))
                return Result<ApplicationDto>.Forbid("No user identifier");

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
                return Result<ApplicationDto>.NotFound("User not found");

            // Check if user has permission to write to this specific application
            var canAccess = permissionCheckerService.HasPermission(
                ResourceType.Application,
                request.ApplicationId.ToString(),
                AccessType.Write);

            if (!canAccess)
                return Result<ApplicationDto>.Forbid("User does not have permission to submit this application");

            // Get the application using query object
            var applicationId = new ApplicationId(request.ApplicationId);
            var application = await (new GetApplicationByIdQueryObject(applicationId))
                .Apply(applicationRepo.Query())
                .FirstOrDefaultAsync(cancellationToken);

            if (application is null)
                return Result<ApplicationDto>.NotFound("Application not found");

            // Check if the current user is the creator of the application
            if (application.CreatedBy != dbUser.Id)
                return Result<ApplicationDto>.Forbid("Only the user who created the application can submit it");

            var now = DateTime.UtcNow;
            application.Submit(now, dbUser.Id!);

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<ApplicationDto>.Success(new ApplicationDto
            {
                ApplicationId = application.Id!.Value,
                ApplicationReference = application.ApplicationReference,
                TemplateVersionId = application.TemplateVersionId.Value,
                TemplateName = application.TemplateVersion?.Template?.Name ?? string.Empty,
                Status = application.Status,
                DateCreated = application.CreatedOn,
                DateSubmitted = application.LastModifiedOn,
                LatestResponse = null,
                TemplateSchema = null
            });
        }
        catch (Exception e)
        {
            return Result<ApplicationDto>.Failure(e.Message);
        }
    }
} 