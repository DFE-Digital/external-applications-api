using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.Commands;

public sealed record AddApplicationResponseCommand(
    Guid ApplicationId,
    string ResponseBody) : IRequest<Result<ApplicationResponseDto>>;

public sealed class AddApplicationResponseCommandHandler(
    IEaRepository<Domain.Entities.Application> applicationRepo,
    IEaRepository<User> userRepo,
    IHttpContextAccessor httpContextAccessor,
    IPermissionCheckerService permissionCheckerService,
    IApplicationFactory applicationFactory,
    IUnitOfWork unitOfWork) : IRequestHandler<AddApplicationResponseCommand, Result<ApplicationResponseDto>>
{
    public async Task<Result<ApplicationResponseDto>> Handle(
        AddApplicationResponseCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<ApplicationResponseDto>.Failure("Not authenticated");

            var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(principalId))
                return Result<ApplicationResponseDto>.Failure("No user identifier");

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
                return Result<ApplicationResponseDto>.Failure("User not found");

            // Check if user has permission to write to this specific application
            var canAccess = permissionCheckerService.HasPermission(ResourceType.Application, request.ApplicationId.ToString(), AccessType.Write);

            if (!canAccess)
                return Result<ApplicationResponseDto>.Failure("User does not have permission to update this application");

            // Get the application using query object
            var applicationId = new ApplicationId(request.ApplicationId);
            var application = await (new GetApplicationByIdQueryObject(applicationId))
                .Apply(applicationRepo.Query())
                .FirstOrDefaultAsync(cancellationToken);

            if (application is null)
                return Result<ApplicationResponseDto>.Failure("Application not found");

            // Add the new response version using factory
            var newResponse = applicationFactory.AddResponseToApplication(application, request.ResponseBody, dbUser.Id!);

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<ApplicationResponseDto>.Success(new ApplicationResponseDto(
                newResponse.Id!.Value,
                newResponse.Application?.ApplicationReference!,
                newResponse.ApplicationId.Value,
                newResponse.ResponseBody,
                newResponse.CreatedOn,
                newResponse.CreatedBy.Value));
        }
        catch (Exception e)
        {
            return Result<ApplicationResponseDto>.Failure(e.Message);
        }
    }
} 