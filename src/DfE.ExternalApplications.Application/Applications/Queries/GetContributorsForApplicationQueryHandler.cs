using DfE.ExternalApplications.Application.Applications.Commands;
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
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed class GetContributorsForApplicationQueryHandler(
    IEaRepository<Domain.Entities.Application> applicationRepo,
    IEaRepository<User> userRepo,
    IHttpContextAccessor httpContextAccessor,
    IPermissionCheckerService permissionCheckerService) : IRequestHandler<GetContributorsForApplicationQuery, Result<IReadOnlyCollection<UserDto>>>
{
    public async Task<Result<IReadOnlyCollection<UserDto>>> Handle(
        GetContributorsForApplicationQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<IReadOnlyCollection<UserDto>>.Failure("Not authenticated");

            var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(principalId))
                return Result<IReadOnlyCollection<UserDto>>.Failure("No user identifier");

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
                return Result<IReadOnlyCollection<UserDto>>.Failure("User not found");

            // Check if user has permission to read this application
            var canAccess = permissionCheckerService.HasPermission(ResourceType.Application, request.ApplicationId.ToString(), AccessType.Read);

            if (!canAccess)
                return Result<IReadOnlyCollection<UserDto>>.Failure("User does not have permission to read this application");

            // Get the application to verify it exists
            var applicationId = new ApplicationId(request.ApplicationId);
            var application = await (new GetApplicationByIdQueryObject(applicationId))
                .Apply(applicationRepo.Query().AsNoTracking())
                .FirstOrDefaultAsync(cancellationToken);

            if (application is null)
                return Result<IReadOnlyCollection<UserDto>>.Failure("Application not found");

            // Get all users with permissions for this application
            var contributors = await userRepo.Query()
                .Include(u => u.Permissions)
                .Include(u => u.Role)
                .Where(u => u.Permissions.Any(p => p.ApplicationId == applicationId && p.ResourceType == ResourceType.Application))
                .ToListAsync(cancellationToken);

            var contributorDtos = contributors.Select(c => new UserDto
            {
                UserId = c.Id!.Value,
                Name = c.Name,
                Email = c.Email,
                RoleId = c.RoleId.Value,
                Authorization = new UserAuthorizationDto
                {
                    Permissions = c.Permissions
                        .Select(p => new UserPermissionDto
                        {
                            ApplicationId = p.ApplicationId?.Value,
                            ResourceType = p.ResourceType,
                            ResourceKey = p.ResourceKey,
                            AccessType = p.AccessType
                        })
                        .ToArray(),
                    Roles = new List<string> { c.Role?.Name! }
                }
            }).ToList().AsReadOnly();

            return Result<IReadOnlyCollection<UserDto>>.Success(contributorDtos);
        }
        catch (Exception e)
        {
            return Result<IReadOnlyCollection<UserDto>>.Failure(e.Message);
        }
    }
} 