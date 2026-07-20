using GovUK.Dfe.CoreLibs.Caching.Helpers;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.FlexForms.Application.Common;
using GovUK.Dfe.FlexForms.Application.Users.QueryObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.Interfaces.Repositories;
using GovUK.Dfe.FlexForms.Domain.Tenancy;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace GovUK.Dfe.FlexForms.Application.Users.Queries
{
    public sealed record GetAllUserPermissionsQuery(UserId UserId)
        : IRequest<Result<UserAuthorizationDto>>;

    public sealed class GetAllUserPermissionsQueryHandler(
        IEaRepository<User> userRepo,
        ICacheService<IRedisCacheType> cacheService,
        ITenantContextAccessor tenantContextAccessor)
        : IRequestHandler<GetAllUserPermissionsQuery, Result<UserAuthorizationDto>>
    {
        public async Task<Result<UserAuthorizationDto>> Handle(
            GetAllUserPermissionsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var baseCacheKey = $"Permissions_All_UserId_{CacheKeyHelper.GenerateHashedCacheKey(request.UserId.Value.ToString())}";
                var cacheKey = TenantCacheKeyHelper.CreateTenantScopedKey(tenantContextAccessor, baseCacheKey);

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

                        var resourcePermissions = userWithPermissions.Permissions
                            .Select(p => new UserPermissionDto
                            {
                                ApplicationId = p.ApplicationId?.Value,
                                ResourceType = p.ResourceType,
                                ResourceKey = p.ResourceKey,
                                AccessType = p.AccessType
                            });

                        var templatePermissions = userWithPermissions.TemplatePermissions
                            .Select(tp => new UserPermissionDto
                            {
                                ResourceType = ResourceType.Template,
                                ResourceKey = tp.TemplateId.Value.ToString(),
                                AccessType = tp.AccessType
                            });

                        var userAuthzDto = new UserAuthorizationDto
                        {
                            Permissions = resourcePermissions.Concat(templatePermissions).ToArray(),
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
