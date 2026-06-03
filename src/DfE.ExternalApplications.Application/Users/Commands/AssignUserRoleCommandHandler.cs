using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Users.Commands;

/// <summary>
/// Assigns an assignable role to a user, creating the user when they do not already exist.
/// </summary>
public sealed record AssignUserRoleCommand(
    string Email,
    string Name,
    string Role,
    IReadOnlyCollection<Guid>? TemplateIds)
    : IRequest<Result<UserDto>>;

/// <summary>
/// Handles administrative assignment of predefined roles to users.
/// </summary>
public sealed class AssignUserRoleCommandHandler(
    IEaRepository<User> userRepo,
    IUnitOfWork unitOfWork,
    IPermissionCheckerService permissionCheckerService,
    IUserRoleProvisionerRegistry roleProvisionerRegistry,
    IHttpContextAccessor httpContextAccessor)
    : IRequestHandler<AssignUserRoleCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(
        AssignUserRoleCommand command,
        CancellationToken cancellationToken)
    {
        if (!permissionCheckerService.IsAdmin())
            return Result<UserDto>.Forbid("Only administrators can assign roles");

        if (string.IsNullOrWhiteSpace(command.Email))
            return Result<UserDto>.Failure("Email is required");

        if (string.IsNullOrWhiteSpace(command.Name))
            return Result<UserDto>.Failure("Name is required");

        if (string.IsNullOrWhiteSpace(command.Role))
            return Result<UserDto>.Failure("Role is required");

        var canonicalRole = RoleNames.ResolveAssignable(command.Role);
        if (canonicalRole is null)
        {
            var allowed = string.Join(", ", RoleNames.Assignable);
            return Result<UserDto>.Failure($"Role '{command.Role}' is not assignable. Allowed roles: {allowed}");
        }

        var provisioner = roleProvisionerRegistry.GetProvisioner(canonicalRole);
        if (provisioner is null)
            return Result<UserDto>.Failure($"No provisioner is registered for role '{canonicalRole}'");

        var templateIds = (command.TemplateIds ?? Array.Empty<Guid>())
            .Select(id => new TemplateId(id))
            .ToList();

        if (provisioner.RequiresTemplateIds && templateIds.Count == 0)
            return Result<UserDto>.Failure($"At least one template ID is required for the {canonicalRole} role");

        var email = command.Email.Trim();
        var now = DateTime.UtcNow;

        var grantedById = await ResolveGrantedByUserIdAsync(cancellationToken);
        if (grantedById is null)
            return Result<UserDto>.Failure("Could not resolve the acting administrator");

        var assignmentRequest = new RoleAssignmentRequest(
            command.Name.Trim(),
            email,
            templateIds,
            grantedById,
            now);

        var existingUser = await (new GetUserWithAllTemplatePermissionsQueryObject(email))
            .Apply(userRepo.Query())
            .Include(u => u.Role)
            .Include(u => u.Permissions)
            .FirstOrDefaultAsync(cancellationToken);

        User user;
        try
        {
            if (existingUser is null)
            {
                user = provisioner.CreateUser(assignmentRequest);
                await userRepo.AddAsync(user, cancellationToken);
            }
            else
            {
                provisioner.AssignToExistingUser(existingUser, assignmentRequest);
                user = existingUser;
            }
        }
        catch (ArgumentException ex)
        {
            return Result<UserDto>.Failure(ex.Message);
        }

        await unitOfWork.CommitAsync(cancellationToken);

        var assignedRoleName = RoleNames.ResolveAssignable(user.Role?.Name ?? canonicalRole) ?? canonicalRole;

        return Result<UserDto>.Success(new UserDto
        {
            UserId = user.Id!.Value,
            Name = user.Name,
            Email = user.Email,
            RoleId = user.RoleId.Value,
            Authorization = new UserAuthorizationDto
            {
                Permissions = user.Permissions.Select(p => new UserPermissionDto
                {
                    ApplicationId = p.ApplicationId?.Value,
                    ResourceType = p.ResourceType,
                    ResourceKey = p.ResourceKey,
                    AccessType = p.AccessType
                }).ToArray(),
                Roles = new[] { assignedRoleName }
            }
        });
    }

    private async Task<UserId?> ResolveGrantedByUserIdAsync(CancellationToken cancellationToken)
    {
        var principal = httpContextAccessor.HttpContext?.User;
        var email = principal?.FindFirstValue(ClaimTypes.Email);
        if (string.IsNullOrWhiteSpace(email))
            return null;

        var adminUser = await (new GetUserByEmailQueryObject(email))
            .Apply(userRepo.Query().AsNoTracking())
            .FirstOrDefaultAsync(cancellationToken);

        return adminUser?.Id;
    }
}
