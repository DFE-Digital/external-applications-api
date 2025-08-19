using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Domain.Factories;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.ExternalApplications.Application.Common.Behaviours;

namespace DfE.ExternalApplications.Application.Applications.Commands;

[RateLimit(2, 30)]
public sealed record AddContributorCommand(
    Guid ApplicationId,
    string Name,
    string Email) : IRequest<Result<UserDto>>, IRateLimitedRequest;

public sealed class AddContributorCommandHandler(
    IEaRepository<Domain.Entities.Application> applicationRepo,
    IEaRepository<User> userRepo,
    IEaRepository<Role> roleRepo,
    IHttpContextAccessor httpContextAccessor,
    IPermissionCheckerService permissionCheckerService,
    IUserFactory userFactory,
    IUnitOfWork unitOfWork) : IRequestHandler<AddContributorCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(
        AddContributorCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<UserDto>.Forbid("Not authenticated");

            var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(principalId))
                return Result<UserDto>.Forbid("No user identifier");

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
                return Result<UserDto>.NotFound("User not found");

            // Get the application to verify it exists
            var applicationId = new ApplicationId(request.ApplicationId);
            var application = await (new GetApplicationByIdQueryObject(applicationId))
                .Apply(applicationRepo.Query().AsNoTracking())
                .FirstOrDefaultAsync(cancellationToken);

            if (application is null)
                return Result<UserDto>.NotFound("Application not found");

            // Check if user is the application owner or admin
            var isOwner = permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString());
            var isAdmin = permissionCheckerService.IsAdmin();

            if (!isOwner && !isAdmin)
                return Result<UserDto>.Forbid("Only the application owner or admin can add contributors");

            // Check if contributor already exists
            var existingContributor = await (new GetUserByEmailQueryObject(request.Email))
                .Apply(userRepo.Query())
                .FirstOrDefaultAsync(cancellationToken);

            if (existingContributor != null)
            {
                return await HandleExistingContributor(existingContributor, applicationId, application, dbUser, cancellationToken);
            }

            // Create new contributor using factory with User role
            var contributorId = new UserId(Guid.NewGuid());
            var now = DateTime.UtcNow;

            var contributor = userFactory.CreateContributor(
                contributorId,
                new RoleId(RoleConstants.UserRoleId),
                request.Name,
                request.Email,
                dbUser.Id!,
                applicationId,
                application.TemplateVersion!.TemplateId,
                now);

            await userRepo.AddAsync(contributor, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            // Create authorization data directly from the contributor instead of querying
            var authorization = CreateAuthorizationFromUser(contributor);

            return Result<UserDto>.Success(new UserDto
            {
                UserId = contributor.Id!.Value,
                Name = contributor.Name,
                Email = contributor.Email,
                RoleId = contributor.RoleId.Value,
                Authorization = authorization
            });
        }
        catch (Exception e)
        {
            return Result<UserDto>.Failure(e.Message);
        }
    }

    private async Task<Result<UserDto>> HandleExistingContributor(
        User existingContributor,
        ApplicationId applicationId,
        Domain.Entities.Application application,
        User dbUser,
        CancellationToken cancellationToken)
    {
        // Check if they already have both Read and Write permissions for this application
        var hasReadPermission = existingContributor.Permissions
            .Any(p => p.ApplicationId == applicationId && p.ResourceType == ResourceType.Application && p.AccessType == AccessType.Read);
        var hasWritePermission = existingContributor.Permissions
            .Any(p => p.ApplicationId == applicationId && p.ResourceType == ResourceType.Application && p.AccessType == AccessType.Write);
        var hasReadFilesPermission = existingContributor.Permissions
            .Any(p => p.ApplicationId == applicationId && p.ResourceType == ResourceType.ApplicationFiles && p.AccessType == AccessType.Read);
        var hasWriteFilesPermission = existingContributor.Permissions
            .Any(p => p.ApplicationId == applicationId && p.ResourceType == ResourceType.ApplicationFiles && p.AccessType == AccessType.Write);

        if (hasReadPermission && hasWritePermission && hasReadFilesPermission && hasWriteFilesPermission)
        {
            // Contributor already exists with all needed permissions, return their details
            var authorization = CreateAuthorizationFromUser(existingContributor);

            return Result<UserDto>.Success(new UserDto
            {
                UserId = existingContributor.Id!.Value,
                Name = existingContributor.Name,
                Email = existingContributor.Email,
                RoleId = existingContributor.RoleId.Value,
                Authorization = authorization
            });
        }

        // Add missing permissions using factory method
        userFactory.AddPermissionToUser(existingContributor, applicationId.Value.ToString(), ResourceType.Application, new[] { AccessType.Read, AccessType.Write }, dbUser.Id!, applicationId);
        userFactory.AddTemplatePermissionToUser(existingContributor, application.TemplateVersion!.TemplateId.Value.ToString(), new[] { AccessType.Read, AccessType.Write }, dbUser.Id!, DateTime.UtcNow);
        userFactory.AddPermissionToUser(
            existingContributor,
            applicationId.Value.ToString(),
            ResourceType.ApplicationFiles,
            new[] { AccessType.Read, AccessType.Write },
            dbUser.Id!,
            applicationId);

        await unitOfWork.CommitAsync(cancellationToken);

        // Create authorization data directly from the updated contributor
        var updatedAuthorization = CreateAuthorizationFromUser(existingContributor);

        return Result<UserDto>.Success(new UserDto
        {
            UserId = existingContributor.Id!.Value,
            Name = existingContributor.Name,
            Email = existingContributor.Email,
            RoleId = existingContributor.RoleId.Value,
            Authorization = updatedAuthorization
        });
    }

    private UserAuthorizationDto? CreateAuthorizationFromUser(User user)
    {
        if (user.Permissions == null || !user.Permissions.Any())
            return null;

        return new UserAuthorizationDto
        {
            Permissions = user.Permissions
                .Select(p => new UserPermissionDto
                {
                    ApplicationId = p.ApplicationId?.Value,
                    ResourceType = p.ResourceType,
                    ResourceKey = p.ResourceKey,
                    AccessType = p.AccessType
                })
                .ToArray(),
            Roles = new List<string> { user.Role?.Name ?? "User" }
        };
    }
} 