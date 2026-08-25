using GovUK.Dfe.CoreLibs.Caching.Helpers;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Common;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Tenancy;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public sealed record GetApplicationsForUserQuery(
    string Email,
    bool IncludeSchema = false,
    Guid? TemplateId = null,
    int? PageNumber = null,
    int? PageSize = null,
    ApplicationListingSearchCriteria? Search = null)
    : IRequest<Result<PagedResult<ApplicationDto>>>;

public sealed class GetApplicationsForUserQueryHandler(
    IEaRepository<User> userRepo,
    IEaRepository<Permission> permissionRepo,
    IEaRepository<Domain.Entities.Application> appRepo,
    ICacheService<IRedisCacheType> cacheService,
    ITenantContextAccessor tenantContextAccessor,
    ITenantTemplateResolver tenantTemplateResolver,
    ILogger<GetApplicationsForUserQueryHandler> logger)
    : IRequestHandler<GetApplicationsForUserQuery, Result<PagedResult<ApplicationDto>>>
{
    public async Task<Result<PagedResult<ApplicationDto>>> Handle(
        GetApplicationsForUserQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var tenantName = tenantContextAccessor.CurrentTenant?.Name ?? "(none)";
            var searchKey = request.Search?.ToCacheKeySuffix() ?? "";
            var baseCacheKey =
                $"Applications_ForUser_{CacheKeyHelper.GenerateHashedCacheKey(request.Email)}_t{request.TemplateId}_{searchKey}_p{request.PageNumber}_ps{request.PageSize}";
            var cacheKey = TenantCacheKeyHelper.CreateTenantScopedKey(tenantContextAccessor, baseCacheKey);
            var methodName = nameof(GetApplicationsForUserQueryHandler);

            return await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var dbUser = await new GetUserByEmailQueryObject(request.Email)
                        .Apply(userRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync(cancellationToken);

                    if (dbUser is null)
                    {
                        logger.LogWarning(
                            "Application listing: user not found. Tenant={Tenant}, Email={Email}",
                            tenantName,
                            request.Email);
                        return Result<PagedResult<ApplicationDto>>.Failure("GetApplicationsForUserQueryHandler > User not found.");
                    }

                    var applicationIds = await new GetApplicationIdsByUserIdQueryObject(dbUser.Id!)
                        .Apply(permissionRepo.Query().AsNoTracking())
                        .Select(p => p.ApplicationId!)
                        .Distinct()
                        .ToListAsync(cancellationToken);

                    var templateIdsFilter = tenantTemplateResolver.ResolveListingTemplateFilter(request.TemplateId);

                    logger.LogInformation(
                        "My applications listing (own applications only). Tenant={Tenant}, Email={Email}, Role={Role}, ExplicitApplicationCount={ApplicationCount}, RequestedTemplateId={RequestedTemplateId}, EffectiveTemplateCount={EffectiveTemplateCount}",
                        tenantName,
                        request.Email,
                        dbUser.Role?.Name ?? "(unknown)",
                        applicationIds.Count,
                        request.TemplateId,
                        templateIdsFilter.Count);

                    var query = ApplicationListingQueryBuilder.BuildMyApplicationsQuery(
                        appRepo,
                        applicationIds,
                        templateIdsFilter);

                    query = ApplicationListingQueryBuilder.ApplySearchFilters(query, request.Search);

                    var pagedResult = await ApplicationListingQueryBuilder.MapPagedResultAsync(
                        query,
                        request.IncludeSchema,
                        request.PageNumber,
                        request.PageSize,
                        cancellationToken);

                    logger.LogInformation(
                        "Application listing completed. Tenant={Tenant}, Email={Email}, ReturnedCount={ReturnedCount}, TotalCount={TotalCount}",
                        tenantName,
                        request.Email,
                        pagedResult.Items.Count,
                        pagedResult.TotalCount);

                    return Result<PagedResult<ApplicationDto>>.Success(pagedResult);
                },
                methodName);
        }
        catch (Exception e)
        {
            logger.LogError(e, "Application listing failed for {Email}", request.Email);
            return Result<PagedResult<ApplicationDto>>.Failure(e.ToString());
        }
    }
}
