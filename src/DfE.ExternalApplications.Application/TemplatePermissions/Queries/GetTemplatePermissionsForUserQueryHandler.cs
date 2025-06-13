using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.TemplatePermissions.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace DfE.ExternalApplications.Application.TemplatePermissions.Queries
{
    public sealed record GetTemplatePermissionsForUserQuery(string Email)
        : IRequest<Result<IReadOnlyCollection<TemplatePermissionDto>>>;

    public sealed class GetTemplatePermissionsForUserQueryHandler(
        IEaRepository<User> userRepo,
        ICacheService<IMemoryCacheType> cacheService)
        : IRequestHandler<GetTemplatePermissionsForUserQuery, Result<IReadOnlyCollection<TemplatePermissionDto>>>
    {
        public async Task<Result<IReadOnlyCollection<TemplatePermissionDto>>> Handle(
            GetTemplatePermissionsForUserQuery request,
            CancellationToken cancellationToken)
        {
            try
            {
                var cacheKey = $"Template_Permissions_{CacheKeyHelper.GenerateHashedCacheKey(request.Email)}";

                var methodName = nameof(GetTemplatePermissionsForUserQueryHandler);

                return await cacheService.GetOrAddAsync(
                    cacheKey,
                    async () =>
                    {
                        var userWithTemplatePermissions =
                            await new GetTemplatePermissionsForUserQueryObject(request.Email)
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