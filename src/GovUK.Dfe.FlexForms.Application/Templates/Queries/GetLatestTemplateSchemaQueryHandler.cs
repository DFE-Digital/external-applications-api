using GovUK.Dfe.CoreLibs.Caching.Helpers;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.FlexForms.Application.Common;
using GovUK.Dfe.FlexForms.Application.Services;
using GovUK.Dfe.FlexForms.Application.Users.QueryObjects;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.Interfaces.Repositories;
using GovUK.Dfe.FlexForms.Domain.Services;
using GovUK.Dfe.FlexForms.Domain.Tenancy;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GovUK.Dfe.FlexForms.Application.Templates.Queries;

public sealed record GetLatestTemplateSchemaQuery(Guid TemplateId)
    : IRequest<Result<TemplateSchemaDto>>;

public sealed class GetLatestTemplateSchemaQueryHandler(
    IHttpContextAccessor httpContextAccessor,
    IEaRepository<User> userRepo,
    IPermissionCheckerService permissionCheckerService,
    ITenantTemplateResolver tenantTemplateResolver,
    ICacheService<IRedisCacheType> cacheService,
    ITenantContextAccessor tenantContextAccessor,
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
                return Result<TemplateSchemaDto>.Forbid("No user identifier");

            if (!await tenantTemplateResolver.IsTemplateInCurrentTenantAsync(
                    new TemplateId(request.TemplateId), cancellationToken))
            {
                return Result<TemplateSchemaDto>.Forbid("Template does not belong to the current tenant");
            }

            var baseCacheKey = $"TemplateSchema_PrincipalId_{CacheKeyHelper.GenerateHashedCacheKey(request.TemplateId.ToString())}_{principalId}";
            var cacheKey = TenantCacheKeyHelper.CreateTenantScopedKey(tenantContextAccessor, baseCacheKey);
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
                        return Result<TemplateSchemaDto>.Forbid("User does not have permission to read this template");

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
