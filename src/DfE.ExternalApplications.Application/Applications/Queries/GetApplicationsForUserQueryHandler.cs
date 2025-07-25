﻿using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record GetApplicationsForUserQuery(string Email, bool IncludeSchema = false)
    : IRequest<Result<IReadOnlyCollection<ApplicationDto>>>;

public sealed class GetApplicationsForUserQueryHandler(
    IEaRepository<User> userRepo,
    IEaRepository<Domain.Entities.Application> appRepo,
    ICacheService<IMemoryCacheType> cacheService)
    : IRequestHandler<GetApplicationsForUserQuery, Result<IReadOnlyCollection<ApplicationDto>>>
{
    public async Task<Result<IReadOnlyCollection<ApplicationDto>>> Handle(
        GetApplicationsForUserQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"Applications_ForUser_{CacheKeyHelper.GenerateHashedCacheKey(request.Email)}";
            var methodName = nameof(GetApplicationsForUserQueryHandler);

            return await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var dbUser = await (new GetUserByEmailQueryObject(request.Email))
                            .Apply(userRepo.Query().AsNoTracking())
                            .FirstOrDefaultAsync(cancellationToken);

                    if (dbUser is null)
                     return Result<IReadOnlyCollection<ApplicationDto>>.Failure("GetApplicationsForUserQueryHandler > User not found.");

                    var userWithPerms = await new GetUserWithAllPermissionsByUserIdQueryObject(dbUser.Id!)
                        .Apply(userRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync(cancellationToken);

                    if (userWithPerms is null)
                        return Result<IReadOnlyCollection<ApplicationDto>>.Success(Array.Empty<ApplicationDto>());

                    var ids = userWithPerms.Permissions
                        .Where(p => p is { ApplicationId: not null, ResourceType: ResourceType.Application })
                        .Select(p => p.ApplicationId!)
                        .Distinct()
                        .ToList();

                    if (!ids.Any())
                        return Result<IReadOnlyCollection<ApplicationDto>>.Success(Array.Empty<ApplicationDto>());

                    var apps = await new GetApplicationsByIdsQueryObject(ids)
                        .Apply(appRepo.Query().AsNoTracking())
                        .ToListAsync(cancellationToken);

                    var dtoList = apps.Select(a => new ApplicationDto
                    {
                        ApplicationId = a.Id!.Value,
                        ApplicationReference = a.ApplicationReference,
                        TemplateVersionId = a.TemplateVersionId.Value,
                        DateCreated = a.CreatedOn,
                        DateSubmitted = a.Status == ApplicationStatus.Submitted ? a.LastModifiedOn : null,
                        Status = a.Status,
                        TemplateSchema = request.IncludeSchema && a.TemplateVersion != null ? new TemplateSchemaDto
                        {
                            TemplateId = a.TemplateVersion.Template?.Id?.Value ?? Guid.Empty,
                            TemplateVersionId = a.TemplateVersion.Id!.Value,
                            VersionNumber = a.TemplateVersion.VersionNumber,
                            JsonSchema = a.TemplateVersion.JsonSchema
                        } : null
                    }).ToList().AsReadOnly();

                    return Result<IReadOnlyCollection<ApplicationDto>>.Success(dtoList);
                },
                methodName);
        }
        catch (Exception e)
        {
            return Result<IReadOnlyCollection<ApplicationDto>>.Failure(e.ToString());
        }
    }
}