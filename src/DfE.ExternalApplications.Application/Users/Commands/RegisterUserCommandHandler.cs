using System.Net;
using System.Security.Claims;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.ExternalApplications.Application.Common.Behaviours;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.Security.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace DfE.ExternalApplications.Application.Users.Commands;

[RateLimit(5, 30)]
public sealed record RegisterUserCommand(string SubjectToken, Guid? TemplateId = null) : IRequest<Result<UserDto>>, IRateLimitedRequest;

public sealed class RegisterUserCommandHandler(
    IEaRepository<User> userRepo,
    IExternalIdentityValidator externalValidator,
    IHttpContextAccessor httpContextAccessor,
    IUserFactory userFactory,
    IUnitOfWork unitOfWork) : IRequestHandler<RegisterUserCommand, Result<UserDto>>
{
    public async Task<Result<UserDto>> Handle(
        RegisterUserCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            // Validate external token and extract claims
            var externalUser = await externalValidator
                .ValidateIdTokenAsync(request.SubjectToken, false, validInternalRequest:false, cancellationToken);

            var email = externalUser.FindFirst(ClaimTypes.Email)?.Value
                        ?? throw new SecurityTokenException("RegisterUserCommandHandler > Missing email");

            var fullName = $"{externalUser.FindFirst(ClaimTypes.GivenName)?.Value} {externalUser.FindFirst(ClaimTypes.Surname)?.Value}";

            var name = externalUser.FindFirst("name")?.Value
                       ?? externalUser.FindFirst("given_name")?.Value
                       ?? email; // Fallback to email if name not available

            if (string.IsNullOrWhiteSpace(fullName))
                fullName = name;

            var now = DateTime.UtcNow;

            // TemplateId is required for new user registration
            if (!request.TemplateId.HasValue)
            {
                // When no TemplateId, only return existing user if they exist
                var existingUser = await (new GetUserByEmailQueryObject(email))
                    .Apply(userRepo.Query().AsNoTracking())
                    .FirstOrDefaultAsync(cancellationToken: cancellationToken);

                if (existingUser is not null)
                {
                    return Result<UserDto>.Success(new UserDto
                    {
                        UserId = existingUser.Id!.Value,
                        Name = existingUser.Name,
                        Email = existingUser.Email,
                        RoleId = existingUser.RoleId.Value,
                        Authorization = CreateAuthorizationFromUser(existingUser)
                    });
                }

                return Result<UserDto>.Failure("Template ID is required for user registration");
            }

            var templateId = new TemplateId(request.TemplateId.Value);

            // Load user by email with template permissions to check access
            var dbUser = await (new GetUserWithAllTemplatePermissionsQueryObject(email))
                .Apply(userRepo.Query().AsNoTracking())
                .Include(u => u.Role)
                .Include(u => u.Permissions)
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (dbUser is not null)
            {
                // User exists: check if they already have permission to the provided template
                var hasTemplatePermission = dbUser.TemplatePermissions
                    .Any(tp => tp.TemplateId.Value == request.TemplateId!.Value);

                if (hasTemplatePermission)
                {
                    // User already has access to this template, return their information
                    return Result<UserDto>.Success(new UserDto
                    {
                        UserId = dbUser.Id!.Value,
                        Name = dbUser.Name,
                        Email = dbUser.Email,
                        RoleId = dbUser.RoleId.Value,
                        Authorization = CreateAuthorizationFromUser(dbUser)
                    });
                }

                // User exists but does not have permission to this template: grant it
                var userToUpdate = await (new GetUserWithAllTemplatePermissionsQueryObject(email))
                    .Apply(userRepo.Query())
                    .Include(u => u.Role)
                    .Include(u => u.Permissions)
                    .FirstOrDefaultAsync(cancellationToken: cancellationToken);

                if (userToUpdate is null)
                    return Result<UserDto>.Failure("User not found for permission update");

                userFactory.EnsureUserHasTemplatePermission(
                    userToUpdate,
                    templateId,
                    userToUpdate.Id!,
                    now);

                await unitOfWork.CommitAsync(cancellationToken);

                return Result<UserDto>.Success(new UserDto
                {
                    UserId = userToUpdate.Id!.Value,
                    Name = userToUpdate.Name,
                    Email = userToUpdate.Email,
                    RoleId = userToUpdate.RoleId.Value,
                    Authorization = CreateAuthorizationFromUser(userToUpdate)
                });
            }

            // Create new user with User role and template permission
            var userId = new UserId(Guid.NewGuid());

            var newUser = userFactory.CreateUser(
                userId,
                new RoleId(RoleConstants.UserRoleId),
                fullName,
                email,
                templateId,
                now);

            await userRepo.AddAsync(newUser, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            // Create authorization data directly from the new user
            var authorization = CreateAuthorizationFromUser(newUser);

            return Result<UserDto>.Success(new UserDto
            {
                UserId = newUser.Id!.Value,
                Name = newUser.Name,
                Email = newUser.Email,
                RoleId = newUser.RoleId.Value,
                Authorization = authorization
            });
        }
        catch (SecurityTokenException ex)
        {
            return Result<UserDto>.Failure($"Invalid token: {ex.Message}");
        }
        catch (Exception ex)
        {
            return Result<UserDto>.Failure(ex.Message);
        }
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