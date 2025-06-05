using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Common.Models;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Users.Queries
{
    public sealed record GetAllUserPermissionsQuery(string Email)
        : IRequest<Result<IReadOnlyCollection<UserPermissionDto>>>;

    public sealed class GetAllUserPermissionsQueryHandler(
        IEaRepository<User> userRepo,
        ICacheService<IMemoryCacheType> cacheService)
        : IRequestHandler<GetAllUserPermissionsQuery, Result<IReadOnlyCollection<UserPermissionDto>>>
    {
        public async Task<Result<IReadOnlyCollection<UserPermissionDto>>> Handle(
            GetAllUserPermissionsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = $"Permissions_All_{CacheKeyHelper.GenerateHashedCacheKey(request.Email)}";

                var methodName = nameof(GetAllUserPermissionsQueryHandler);

                return await cacheService.GetOrAddAsync(
                    cacheKey,
                    async () =>
                    {
                        var userWithPermissions = await new GetUserWithAllPermissionsQueryObject(request.Email)
                            .Apply(userRepo.Query().AsNoTracking())
                            .FirstOrDefaultAsync(cancellationToken);

                        if (userWithPermissions is null)
                        {
                            return Result<IReadOnlyCollection<UserPermissionDto>>.Success(Array.Empty<UserPermissionDto>());
                        }

                        var dtoList = userWithPermissions.Permissions
                            .Select(p => new UserPermissionDto
                            {
                                ApplicationId = p.ApplicationId.Value,
                                ResourceKey = p.ResourceKey,
                                AccessType = p.AccessType
                            })
                            .ToList()
                            .AsReadOnly();

                        return Result<IReadOnlyCollection<UserPermissionDto>>.Success(dtoList);
                    },
                    methodName);
            }
            catch (Exception e)
            {
                return Result<IReadOnlyCollection<UserPermissionDto>>.Failure(e.ToString());
            }
        }
    }
}
