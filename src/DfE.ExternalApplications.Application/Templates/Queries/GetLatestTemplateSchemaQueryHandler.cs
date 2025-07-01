using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Templates.Queries;

public sealed record GetLatestTemplateSchemaQuery(Guid TemplateId)
    : IRequest<Result<TemplateSchemaDto>>;

public sealed class GetLatestTemplateSchemaQueryHandler(
    IHttpContextAccessor httpContextAccessor,
    IEaRepository<User> userRepo,
    IPermissionCheckerService permissionCheckerService,
    ICacheService<IMemoryCacheType> cacheService,
    ISender mediator)
    : IRequestHandler<GetLatestTemplateSchemaQuery, Result<TemplateSchemaDto>>
{
    public async Task<Result<TemplateSchemaDto>> Handle(
        GetLatestTemplateSchemaQuery request,
        CancellationToken cancellationToken)
    {
        try
        {
            var httpContext = httpContextAccessor.HttpContext;
            if (httpContext?.User is not { } user || !user.Identity?.IsAuthenticated == true)
                return Result<TemplateSchemaDto>.Failure("Not authenticated");

            var principalId = user.FindFirstValue("appid") ?? user.FindFirstValue("azp");

            if (string.IsNullOrEmpty(principalId))
                principalId = user.FindFirstValue(ClaimTypes.Email);

            if (string.IsNullOrEmpty(principalId))
                return Result<TemplateSchemaDto>.Failure("No user identifier");

            var cacheKey = $"TemplateSchema_PrincipalId_{CacheKeyHelper.GenerateHashedCacheKey(request.TemplateId.ToString())}_{principalId}";
            var methodName = nameof(GetLatestTemplateSchemaQueryHandler);

            return await cacheService.GetOrAddAsync(
                cacheKey,
                async () =>
                {
                    User? dbUser;
                    if (principalId.Contains('@'))
                    {
                        dbUser = await (new GetUserByEmailQueryObject(principalId))
                            .Apply(userRepo.Query().AsNoTracking())
                            .FirstOrDefaultAsync(cancellationToken);
                    }
                    else
                    {
                        dbUser = await (new GetUserByExternalProviderIdQueryObject(principalId))
                            .Apply(userRepo.Query().AsNoTracking())
                            .FirstOrDefaultAsync(cancellationToken);
                    }

                    var canAccess = permissionCheckerService.HasPermission(ResourceType.Template, request.TemplateId.ToString(), AccessType.Read);

                    if (!canAccess)
                        return Result<TemplateSchemaDto>.Failure("User does not have permission to read this template");

                    return await mediator.Send(
                        new GetLatestTemplateSchemaByUserIdQuery(request.TemplateId, dbUser.Id!),
                        cancellationToken);
                },
                methodName);
        }
        catch (Exception e)
        {
            return Result<TemplateSchemaDto>.Failure(e.ToString());
        }
    }
}
