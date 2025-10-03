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
                .ValidateIdTokenAsync(request.SubjectToken, cancellationToken);

            var email = externalUser.FindFirst(ClaimTypes.Email)?.Value
                        ?? throw new SecurityTokenException("RegisterUserCommandHandler > Missing email");

            var name = externalUser.FindFirst(ClaimTypes.Name)?.Value 
                       ?? externalUser.FindFirst("name")?.Value
                       ?? email; // Fallback to email if name not available

            // Check if user already exists
            var dbUser = await (new GetUserByEmailQueryObject(email))
                .Apply(userRepo.Query().AsNoTracking())
                .FirstOrDefaultAsync(cancellationToken: cancellationToken);

            if (dbUser is not null)
            {
                // User already exists, return their information
                return Result<UserDto>.Success(new UserDto
                {
                    UserId = dbUser.Id!.Value,
                    Name = dbUser.Name,
                    Email = dbUser.Email,
                    RoleId = dbUser.RoleId.Value,
                    Authorization = CreateAuthorizationFromUser(dbUser)
                });
            }

            // Create new user with User role
            var userId = new UserId(Guid.NewGuid());
            var now = DateTime.UtcNow;

            var templateId = request.TemplateId.HasValue 
                ? new TemplateId(request.TemplateId.Value) 
                : null;

            var newUser = userFactory.CreateUser(
                userId,
                new RoleId(RoleConstants.UserRoleId),
                name,
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