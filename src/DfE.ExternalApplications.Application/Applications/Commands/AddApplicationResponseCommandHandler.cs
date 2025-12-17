using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Application.Common.Attributes;
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
using DfE.ExternalApplications.Application.Common.Behaviours;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.Commands;

[RateLimit(10, 30)]
public sealed record AddApplicationResponseCommand(
    Guid ApplicationId,
    string ResponseBody) : IRequest<Result<ApplicationResponseDto>>, IRateLimitedRequest;

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
                return Result<ApplicationResponseDto>.Forbid("Not authenticated");

            var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(principalId))
                return Result<ApplicationResponseDto>.Forbid("No user identifier");

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
                return Result<ApplicationResponseDto>.NotFound("User not found");

            // Check if user has permission to write to this specific application
            var canAccess = permissionCheckerService.HasPermission(ResourceType.Application, request.ApplicationId.ToString(), AccessType.Write);

            if (!canAccess)
                return Result<ApplicationResponseDto>.Forbid("User does not have permission to update this application");

            // Get the application using a lightweight query object (avoid loading large navigation graphs like Responses)
            var applicationId = new ApplicationId(request.ApplicationId);
            var application = await (new GetApplicationByIdForResponseWriteQueryObject(applicationId))
                .Apply(applicationRepo.Query()) // tracked - required for update + new response insert
                .FirstOrDefaultAsync(cancellationToken);

            if (application is null)
                return Result<ApplicationResponseDto>.NotFound("Application not found");

            var decodedResponseBody = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(request.ResponseBody));

            // Add the new response version using factory
            var newResponse = applicationFactory.AddResponseToApplication(application, decodedResponseBody, dbUser.Id!);

            await unitOfWork.CommitAsync(cancellationToken);

            return Result<ApplicationResponseDto>.Success(new ApplicationResponseDto(
                newResponse.Id!.Value,
                newResponse.Application?.ApplicationReference!,
                newResponse.ApplicationId.Value,
                newResponse.ResponseBody,
                newResponse.CreatedOn,
                newResponse.CreatedBy.Value));
        }
        catch (FormatException)
        {
            return Result<ApplicationResponseDto>.Failure("Invalid Base64 format for ResponseBody");
        }
        catch (Exception e)
        {
            return Result<ApplicationResponseDto>.Failure(e.Message);
        }
    }
} 