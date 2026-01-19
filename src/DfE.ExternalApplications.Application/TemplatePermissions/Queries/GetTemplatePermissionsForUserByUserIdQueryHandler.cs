using GovUK.Dfe.CoreLibs.Caching.Helpers;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Common;
using DfE.ExternalApplications.Application.TemplatePermissions.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.TemplatePermissions.Queries
{
    public sealed record GetTemplatePermissionsForUserByUserIdQuery(UserId UserId)
        : IRequest<Result<IReadOnlyCollection<TemplatePermissionDto>>>;

    public sealed class GetTemplatePermissionsForUserByUserIdQueryHandler(
        IEaRepository<User> userRepo,
        ICacheService<IRedisCacheType> cacheService,
        ITenantContextAccessor tenantContextAccessor)
        : IRequestHandler<GetTemplatePermissionsForUserByUserIdQuery, Result<IReadOnlyCollection<TemplatePermissionDto>>>
    {
        public async Task<Result<IReadOnlyCollection<TemplatePermissionDto>>> Handle(
            GetTemplatePermissionsForUserByUserIdQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var baseCacheKey = $"Template_Permissions_ByUiD_{CacheKeyHelper.GenerateHashedCacheKey(request.UserId.Value.ToString())}";
                var cacheKey = TenantCacheKeyHelper.CreateTenantScopedKey(tenantContextAccessor, baseCacheKey);

                var methodName = nameof(GetTemplatePermissionsForUserByUserIdQueryHandler);

                return await cacheService.GetOrAddAsync(
                    cacheKey,
                    async () =>
                    {
                        var userWithTemplatePermissions =
                            await new GetTemplatePermissionsForUserByUserIdQueryObject(request.UserId)
                                .Apply(userRepo.Query().AsNoTracking())
                                .FirstOrDefaultAsync(cancellationToken);

                        if (userWithTemplatePermissions is null)
                        {
                            return Result<IReadOnlyCollection<TemplatePermissionDto>>.Success(
                                Array.Empty<TemplatePermissionDto>());
                        }

                        var dtoList = userWithTemplatePermissions.TemplatePermissions
                            .Select(p => new TemplatePermissionDto
                            {
                                TemplatePermissionId = p.Id?.Value,
                                UserId = p.UserId.Value,
                                TemplateId = p.TemplateId.Value,
                                AccessType = p.AccessType
                            })
                            .ToList()
                            .AsReadOnly();

                        return Result<IReadOnlyCollection<TemplatePermissionDto>>.Success(dtoList);
                    },
                    methodName);
            }
            catch (Exception e)
            {
                return Result<IReadOnlyCollection<TemplatePermissionDto>>.Failure(e.ToString());
            }
        }
    }
}