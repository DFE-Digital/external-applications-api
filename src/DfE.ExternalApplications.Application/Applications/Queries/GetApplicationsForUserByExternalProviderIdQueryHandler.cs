using GovUK.Dfe.CoreLibs.Caching.Helpers;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Application.Common;
using DfE.ExternalApplications.Application.Common.QueriesObjects;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record GetApplicationsForUserByExternalProviderIdQuery(
    string ExternalProviderId,
    bool IncludeSchema = false,
    Guid? TemplateId = null,
    int? PageNumber = null,
    int? PageSize = null,
    string? SearchReference = null)
    : IRequest<Result<PagedResult<ApplicationDto>>>;

public sealed class GetApplicationsForUserByExternalProviderIdQueryHandler(
    IEaRepository<User> userRepo,
    IEaRepository<Domain.Entities.Application> appRepo,
    ICacheService<IRedisCacheType> cacheService,
    ITenantContextAccessor tenantContextAccessor)
    : IRequestHandler<GetApplicationsForUserByExternalProviderIdQuery, Result<PagedResult<ApplicationDto>>>
{
    public async Task<Result<PagedResult<ApplicationDto>>> Handle(
        GetApplicationsForUserByExternalProviderIdQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var baseCacheKey = $"Applications_ForUserExternal_{CacheKeyHelper.GenerateHashedCacheKey(request.ExternalProviderId)}_sr{request.SearchReference ?? ""}_p{request.PageNumber}_ps{request.PageSize}";
            var cacheKey = TenantCacheKeyHelper.CreateTenantScopedKey(tenantContextAccessor, baseCacheKey);
            var methodName = nameof(GetApplicationsForUserByExternalProviderIdQueryHandler);

            return await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var userWithPerms = await new GetUserWithAllPermissionsByExternalIdQueryObject(request.ExternalProviderId)
                        .Apply(userRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync(cancellationToken);

                    if (userWithPerms is null)
                        return Result<PagedResult<ApplicationDto>>.Success(EmptyPagedResult(request));

                    var ids = userWithPerms.Permissions
                        .Where(p => p.ApplicationId != null)
                        .Select(p => p.ApplicationId!)
                        .Distinct()
                        .ToList();

                    if (!ids.Any())
                        return Result<PagedResult<ApplicationDto>>.Success(EmptyPagedResult(request));

                    var query = new GetApplicationsByIdsQueryObject(ids)
                        .Apply(appRepo.Query().AsNoTracking());

                    // Apply template filter if specified
                    if (request.TemplateId.HasValue)
                        query = new GetApplicationsByTemplateIdQueryObject(new TemplateId(request.TemplateId.Value))
                            .Apply(query);

                    // Apply reference search filter if specified
                    if (!string.IsNullOrWhiteSpace(request.SearchReference))
                        query = new GetApplicationsByReferenceSearchQueryObject(request.SearchReference)
                            .Apply(query);

                    int? totalCount = null;
                    if (request.PageNumber.HasValue && request.PageSize.HasValue)
                    {
                        totalCount = await query.CountAsync(cancellationToken);
                        var pageIndex = Math.Max(0, request.PageNumber.Value - 1);
                        query = new PagingQuery<Domain.Entities.Application>(pageIndex, request.PageSize.Value)
                            .Apply(query);
                    }

                    var apps = await query.ToListAsync(cancellationToken);
                    var count = totalCount ?? apps.Count;

                    var dtoList = apps.Select(a => new ApplicationDto
                    {
                        ApplicationId = a.Id!.Value,
                        ApplicationReference = a.ApplicationReference,
                        TemplateVersionId = a.TemplateVersionId.Value,
                        DateCreated = a.CreatedOn,
                        DateSubmitted = a.Status == ApplicationStatus.Submitted ? a.LastModifiedOn : null,
                        TemplateName = a.TemplateVersion?.Template?.Name ?? string.Empty,
                        Status = a.Status,
                        TemplateSchema = request.IncludeSchema && a.TemplateVersion != null ? new TemplateSchemaDto
                        {
                            TemplateId = a.TemplateVersion.Template?.Id?.Value ?? Guid.Empty,
                            TemplateVersionId = a.TemplateVersion.Id!.Value,
                            VersionNumber = a.TemplateVersion.VersionNumber,
                            JsonSchema = a.TemplateVersion.JsonSchema
                        } : null
                    }).ToList().AsReadOnly();

                    var effectivePageSize = request.PageSize ?? count;
                    var effectivePage = request.PageNumber ?? 1;
                    var totalPages = effectivePageSize > 0
                        ? (int)Math.Ceiling((double)count / effectivePageSize)
                        : 1;

                    return Result<PagedResult<ApplicationDto>>.Success(new PagedResult<ApplicationDto>
                    {
                        Items = dtoList,
                        TotalCount = count,
                        PageNumber = effectivePage,
                        PageSize = effectivePageSize,
                        TotalPages = totalPages
                    });
                },
                methodName);
        }
        catch (Exception e)
        {
            return Result<PagedResult<ApplicationDto>>.Failure(e.ToString());
        }
    }

    private static PagedResult<ApplicationDto> EmptyPagedResult(GetApplicationsForUserByExternalProviderIdQuery request) =>
        new()
        {
            Items = Array.Empty<ApplicationDto>(),
            TotalCount = 0,
            PageNumber = request.PageNumber ?? 1,
            PageSize = request.PageSize ?? 0,
            TotalPages = 0
        };
}
