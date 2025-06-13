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
    public sealed record GetAllUserTemplatePermissionsQuery(string Email)
        : IRequest<Result<IReadOnlyCollection<TemplatePermissionDto>>>;

    public sealed class GetAllUserTemplatePermissionsQueryHandler(
        IEaRepository<User> userRepo,
        ICacheService<IMemoryCacheType> cacheService)
        : IRequestHandler<GetAllUserTemplatePermissionsQuery, Result<IReadOnlyCollection<TemplatePermissionDto>>>
    {
        public async Task<Result<IReadOnlyCollection<TemplatePermissionDto>>> Handle(
            GetAllUserTemplatePermissionsQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = $"Template_Permissions_{CacheKeyHelper.GenerateHashedCacheKey(request.Email)}";

                var methodName = nameof(GetAllUserTemplatePermissionsQueryHandler);

                return await cacheService.GetOrAddAsync(
                    cacheKey,
                    async () =>
                    {
                        var userWithTemplatePermissions = await new GetUserWithAllTemplatePermissionsQueryObject(request.Email)
                            .Apply(userRepo.Query().AsNoTracking())
                            .FirstOrDefaultAsync(cancellationToken);

                        if (userWithTemplatePermissions is null)
                        {
                            return Result<IReadOnlyCollection<TemplatePermissionDto>>.Success(Array.Empty<TemplatePermissionDto>());
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