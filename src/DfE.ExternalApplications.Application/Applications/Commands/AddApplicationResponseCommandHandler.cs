using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DfE.ExternalApplications.Application.Common.Behaviours;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Applications.Commands;

[RateLimit(5, 30)]
public sealed record AddApplicationResponseCommand(
    Guid ApplicationId,
    string ResponseBody) : IRequest<Result<ApplicationResponseDto>>, IRateLimitedRequest;

public sealed class AddApplicationResponseCommandHandler(
    IEaRepository<User> userRepo,
    IHttpContextAccessor httpContextAccessor,
    IPermissionCheckerService permissionCheckerService,
    IApplicationRepository applicationRepository,
    IApplicationResponseAppender responseAppender,
    IMediator mediator) : IRequestHandler<AddApplicationResponseCommand, Result<ApplicationResponseDto>>
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

            var applicationId = new ApplicationId(request.ApplicationId);
            var decodedResponseBody = System.Text.Encoding.UTF8.GetString(System.Convert.FromBase64String(request.ResponseBody));

            // Keep invariants + event payload creation in the Domain.
            var append = responseAppender.Create(applicationId, decodedResponseBody, dbUser.Id!);

            // Persist the aggregate change using the Application (aggregate root) repository without loading the graph.
            var persisted = await applicationRepository.AppendResponseVersionAsync(
                applicationId,
                append.Response,
                append.Now,
                dbUser.Id!,
                cancellationToken);

            if (persisted is null)
                return Result<ApplicationResponseDto>.NotFound("Application not found");

            // Publish the domain event payload (originated from Domain service).
            await mediator.Publish(append.DomainEvent, cancellationToken);

            var (applicationReference, newResponse) = persisted.Value;

            return Result<ApplicationResponseDto>.Success(new ApplicationResponseDto(
                newResponse.Id!.Value,
                applicationReference,
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