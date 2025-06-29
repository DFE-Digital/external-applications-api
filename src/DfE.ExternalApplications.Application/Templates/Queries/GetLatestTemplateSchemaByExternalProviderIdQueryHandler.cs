using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Templates.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Templates.Queries;

public sealed record GetLatestTemplateSchemaByExternalProviderIdQuery(Guid TemplateId, string ExternalProviderId)
    : IRequest<Result<TemplateSchemaDto>>;

public sealed class GetLatestTemplateSchemaByExternalProviderIdQueryHandler(
    IEaRepository<TemplatePermission> accessRepo,
    IEaRepository<TemplateVersion> versionRepo,
    ICacheService<IMemoryCacheType> cacheService)
    : IRequestHandler<GetLatestTemplateSchemaByExternalProviderIdQuery, Result<TemplateSchemaDto>>
{
    public async Task<Result<TemplateSchemaDto>> Handle(
        GetLatestTemplateSchemaByExternalProviderIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"TemplateSchema_{CacheKeyHelper.GenerateHashedCacheKey(request.TemplateId.ToString())}_{request.ExternalProviderId}";
            var methodName = nameof(GetLatestTemplateSchemaByExternalProviderIdQueryHandler);

            return await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var access = await new GetTemplatePermissionByExternalProviderIdQueryObject(request.ExternalProviderId, request.TemplateId)
                        .Apply(accessRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync(cancellationToken);

                    if (access is null)
                        return Result<TemplateSchemaDto>.Failure("Access denied");


                    var latest = await new GetLatestTemplateVersionForTemplateQueryObject(access.TemplateId)
                        .Apply(versionRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync(cancellationToken);

                    if (latest is null)
                    {
                        return Result<TemplateSchemaDto>.Failure("Template version not found");
                    }

                    return Result<TemplateSchemaDto>.Success(new TemplateSchemaDto
                    {
                        TemplateVersionId = latest.Id!.Value,
                        JsonSchema = latest.JsonSchema,
                        TemplateId = latest.TemplateId.Value,
                        VersionNumber = latest.VersionNumber,
                    });
                },
                methodName);
        }
        catch (Exception e)
        {
            return Result<TemplateSchemaDto>.Failure(e.ToString());
        }
    }
} 