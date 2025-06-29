using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Templates.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Templates.Queries;

public sealed record GetLatestTemplateSchemaQuery(Guid TemplateId, string Email)
    : IRequest<Result<TemplateSchemaDto>>;

public sealed class GetLatestTemplateSchemaQueryHandler(
    IEaRepository<TemplatePermission> accessRepo,
    IEaRepository<TemplateVersion> versionRepo,
    ICacheService<IMemoryCacheType> cacheService)
    : IRequestHandler<GetLatestTemplateSchemaQuery, Result<TemplateSchemaDto>>
{
    public async Task<Result<TemplateSchemaDto>> Handle(
        GetLatestTemplateSchemaQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var cacheKey = $"TemplateSchema_{CacheKeyHelper.GenerateHashedCacheKey(request.TemplateId.ToString())}_{request.Email}";
            var methodName = nameof(GetLatestTemplateSchemaQueryHandler);

            return await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    // First check if the template exists
                    var latest = await new GetLatestTemplateVersionForTemplateQueryObject(new TemplateId(request.TemplateId))
                        .Apply(versionRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync(cancellationToken);

                    if (latest is null)
                    {
                        return Result<TemplateSchemaDto>.Failure("Template version not found");
                    }

                    // Then check if the user has access
                    var access = await new GetTemplatePermissionByTemplateNameQueryObject(request.Email, request.TemplateId)
                        .Apply(accessRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync(cancellationToken);

                    if (access is null)
                    {
                        return Result<TemplateSchemaDto>.Failure("Access denied");
                    }

                    var dto = new TemplateSchemaDto
                    {
                        VersionNumber = latest.VersionNumber,
                        JsonSchema = latest.JsonSchema,
                        TemplateId = latest.TemplateId.Value,
                        TemplateVersionId = latest.Id!.Value,
                    };

                    return Result<TemplateSchemaDto>.Success(dto);
                },
                methodName);
        }
        catch (Exception e)
        {
            return Result<TemplateSchemaDto>.Failure(e.ToString());
        }
    }
}
