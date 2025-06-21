using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Users.Queries
{
    public sealed record GetAllUserPermissionsByExternalProviderIdQuery(string ExternalProviderId)
        : IRequest<Result<IReadOnlyCollection<UserPermissionDto>>>;

    public sealed class GetAllUserPermissionsByExternalProviderIdQueryHandler(
        IEaRepository<User> userRepo,
        ICacheService<IMemoryCacheType> cache)
        : IRequestHandler<GetAllUserPermissionsByExternalProviderIdQuery,
            Result<IReadOnlyCollection<UserPermissionDto>>>
    {
        public async Task<Result<IReadOnlyCollection<UserPermissionDto>>> Handle(
            GetAllUserPermissionsByExternalProviderIdQuery request,
            CancellationToken cancellationToken)
        {
            var cacheKey = $"Permissions_ByExternalId_{CacheKeyHelper.GenerateHashedCacheKey(request.ExternalProviderId)}";

            return await cache.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    var userWithPerms = await new GetUserWithAllPermissionsByExternalIdQueryObject(request.ExternalProviderId)
                        .Apply(userRepo.Query().AsNoTracking())
                        .FirstOrDefaultAsync(cancellationToken);

                    if (userWithPerms is null)
                        return Result<IReadOnlyCollection<UserPermissionDto>>.Success(
                            Array.Empty<UserPermissionDto>());

                    var dtoList = userWithPerms.Permissions
                        .Select(p => new UserPermissionDto
                        {
                            ApplicationId = p.ApplicationId?.Value,
                            ResourceType = p.ResourceType,
                            ResourceKey = p.ResourceKey,
                            AccessType = p.AccessType
                        })
                        .ToList()
                        .AsReadOnly();

                    return Result<IReadOnlyCollection<UserPermissionDto>>.Success(dtoList);
                },
                nameof(GetAllUserPermissionsByExternalProviderIdQueryHandler));
        }
    }

}
