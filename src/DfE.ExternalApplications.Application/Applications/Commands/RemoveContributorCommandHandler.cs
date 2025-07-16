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
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.Commands;

public sealed class RemoveContributorCommandHandler(
    IEaRepository<Domain.Entities.Application> applicationRepo,
    IEaRepository<User> userRepo,
    IHttpContextAccessor httpContextAccessor,
    IPermissionCheckerService permissionCheckerService,
    IUnitOfWork unitOfWork) : IRequestHandler<RemoveContributorCommand, Result<bool>>
{
    public async Task<Result<bool>> Handle(
        RemoveContributorCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<bool>.Failure("Not authenticated");

            var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(principalId))
                return Result<bool>.Failure("No user identifier");

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
                return Result<bool>.Failure("User not found");

            // Get the application to verify it exists
            var applicationId = new ApplicationId(request.ApplicationId);
            var application = await (new GetApplicationByIdQueryObject(applicationId))
                .Apply(applicationRepo.Query().AsNoTracking())
                .FirstOrDefaultAsync(cancellationToken);

            if (application is null)
                return Result<bool>.Failure("Application not found");

            // Check if user is the application owner or admin
            var isOwner = permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString());
            var isAdmin = permissionCheckerService.IsAdmin();

            if (!isOwner && !isAdmin)
                return Result<bool>.Failure("Only the application owner or admin can remove contributors");

            // Get the contributor to remove
            var contributorId = new UserId(request.UserId);
            var contributor = await (new GetUserWithAllPermissionsByUserIdQueryObject(contributorId))
                .Apply(userRepo.Query())
                .FirstOrDefaultAsync(cancellationToken);

            if (contributor is null)
                return Result<bool>.Failure("Contributor not found");

            // Check if contributor has permission for this application
            var permission = contributor.Permissions
                .FirstOrDefault(p => p.ApplicationId == applicationId && p.ResourceType == ResourceType.Application);

            if (permission is null)
                return Result<bool>.Failure("Contributor does not have permission for this application");

            // Remove the permission
            contributor.Permissions.ToList().Remove(permission);

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<bool>.Success(true);
        }
        catch (Exception e)
        {
            return Result<bool>.Failure(e.Message);
        }
    }
} 