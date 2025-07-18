using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.Queries;
public sealed record GetContributorsForApplicationQuery(
    Guid ApplicationId,
    bool IncludePermissionDetails = false) : IRequest<Result<IReadOnlyCollection<UserDto>>>;

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

            // Get the application to verify it exists
            var applicationId = new ApplicationId(request.ApplicationId);
            var application = await (new GetApplicationByIdQueryObject(applicationId))
                .Apply(applicationRepo.Query().AsNoTracking())
                .FirstOrDefaultAsync(cancellationToken);

            if (application is null)
                return Result<IReadOnlyCollection<UserDto>>.Failure("Application not found");

            // Check if user is the application owner or admin
            var isOwner = permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString());
            var isAdmin = permissionCheckerService.IsAdmin();

            if (!isOwner && !isAdmin)
                return Result<IReadOnlyCollection<UserDto>>.Failure("Only the application owner or admin can view contributors");

            // Get all contributors
            var contributors = await (new GetContributorsForApplicationQueryObject(applicationId))
                .Apply(userRepo.Query().AsNoTracking())
                .ToListAsync(cancellationToken);

            var contributorDtos = contributors.Select(c => new UserDto
            {
                UserId = c.Id!.Value,
                Name = c.Name,
                Email = c.Email,
                RoleId = c.RoleId.Value,
                Authorization = request.IncludePermissionDetails ? new UserAuthorizationDto
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
                } : null
            }).ToList().AsReadOnly();

            return Result<IReadOnlyCollection<UserDto>>.Success(contributorDtos);
        }
        catch (Exception e)
        {
            return Result<IReadOnlyCollection<UserDto>>.Failure(e.Message);
        }
    }
} 