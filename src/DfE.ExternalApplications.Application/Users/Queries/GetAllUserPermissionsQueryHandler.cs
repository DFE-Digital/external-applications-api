﻿using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.Users.Queries
{
    public sealed record GetAllUserPermissionsQuery(UserId UserId)
        : IRequest<Result<UserAuthorizationDto>>;

    public sealed class GetAllUserPermissionsQueryHandler(
        IEaRepository<User> userRepo,
        ICacheService<IMemoryCacheType> cacheService)
        : IRequestHandler<GetAllUserPermissionsQuery, Result<UserAuthorizationDto>>
    {
        public async Task<Result<UserAuthorizationDto>> Handle(
            GetAllUserPermissionsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = $"Permissions_All_UserId_{CacheKeyHelper.GenerateHashedCacheKey(request.UserId.Value.ToString())}";

                var methodName = nameof(GetAllUserPermissionsQueryHandler);

                return await cacheService.GetOrAddAsync(
                    cacheKey,
                    async () =>
                    {
                        var userWithPermissions = await new GetUserWithAllPermissionsByUserIdQueryObject(request.UserId)
                            .Apply(userRepo.Query().AsNoTracking())
                            .FirstOrDefaultAsync(cancellationToken);

                        if (userWithPermissions is null)
                        {
                            return Result<UserAuthorizationDto>.Success(new UserAuthorizationDto()
                            {
                                Permissions = Array.Empty<UserPermissionDto>(),
                                Roles = Array.Empty<string>(),
                            });
                        }

                        var userAuthzDto = new UserAuthorizationDto
                        {
                            Permissions = userWithPermissions.Permissions
                                .Select(p => new UserPermissionDto
                                {
                                    ApplicationId = p.ApplicationId?.Value,
                                    ResourceType = p.ResourceType,
                                    ResourceKey = p.ResourceKey,
                                    AccessType = p.AccessType
                                })
                                .ToArray(),
                            Roles = new List<string>(){ userWithPermissions.Role?.Name! }
                        };

                        return Result<UserAuthorizationDto>.Success(userAuthzDto);
                    },
                    methodName);
            }
            catch (Exception e)
            {
                return Result<UserAuthorizationDto>.Failure(e.ToString());
            }
        }
    }
}
