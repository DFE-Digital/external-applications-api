using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Templates.Queries;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Factories;
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
using DfE.ExternalApplications.Application.Common.Behaviours;
using DfE.ExternalApplications.Application.Common.Attributes;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Caching.Helpers;

namespace DfE.ExternalApplications.Application.Applications.Commands;

[RateLimit(1, 30)]
public sealed record CreateApplicationCommand(
    Guid TemplateId,
    string InitialResponseBody) : IRequest<Result<ApplicationDto>>, IRateLimitedRequest;

public sealed class CreateApplicationCommandHandler(
    IEaRepository<Domain.Entities.Application> applicationRepo,
    IEaRepository<User> userRepo,
    IHttpContextAccessor httpContextAccessor,
    IApplicationReferenceProvider referenceProvider,
    IApplicationFactory applicationFactory,
    IPermissionCheckerService permissionCheckerService,
    ISender mediator,
    ICacheService<IMemoryCacheType> cacheService,
    IUnitOfWork unitOfWork) : IRequestHandler<CreateApplicationCommand, Result<ApplicationDto>>
{
    public async Task<Result<ApplicationDto>> Handle(
        CreateApplicationCommand request,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not ClaimsPrincipal user || !user.Identity?.IsAuthenticated == true)
                return Result<ApplicationDto>.Forbid("Not authenticated");

            var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(principalId))
                return Result<ApplicationDto>.Forbid("No user identifier");

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
                return Result<ApplicationDto>.NotFound("User not found");

            var canAccess = permissionCheckerService.HasPermission(ResourceType.Template, request.TemplateId.ToString(), AccessType.Write);

            if (!canAccess)
                return Result<ApplicationDto>.Forbid("User does not have permission to create applications for this template");

            var templateSchemaResult = await mediator.Send(
                new GetLatestTemplateSchemaByUserIdQuery(request.TemplateId, dbUser.Id!),
                cancellationToken);

            if (!templateSchemaResult.IsSuccess)
                return Result<ApplicationDto>.Failure(templateSchemaResult.Error!);

            var reference = await referenceProvider.GenerateReferenceAsync(cancellationToken);
            var applicationId = new ApplicationId(Guid.NewGuid());
            var now = DateTime.UtcNow;

            var (application, _) = applicationFactory.CreateApplicationWithResponse(
                applicationId,
                reference,
                new TemplateVersionId(templateSchemaResult.Value!.TemplateVersionId),
                request.InitialResponseBody,
                now,
                dbUser.Id!);

            await applicationRepo.AddAsync(application, cancellationToken);
            await unitOfWork.CommitAsync(cancellationToken);

            // invalidate the user claim cache
            cacheService.Remove($"UserClaims_{CacheKeyHelper.GenerateHashedCacheKey(dbUser.Email.ToLower())}");

            return Result<ApplicationDto>.Success(new ApplicationDto
            {
                ApplicationId = application.Id!.Value,
                ApplicationReference = application.ApplicationReference,
                TemplateVersionId = application.TemplateVersionId.Value,
                DateCreated = application.CreatedOn,
                Status = application.Status,
                TemplateSchema = templateSchemaResult.Value
            });
        }
        catch (Exception e)
        {
            return Result<ApplicationDto>.Failure(e.Message);
        }
    }
} 