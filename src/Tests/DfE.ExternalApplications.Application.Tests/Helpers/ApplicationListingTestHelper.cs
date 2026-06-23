using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Helpers;

internal static class ApplicationListingTestHelper
{
    internal static ITenantTemplateResolver CreateEmptyTemplateResolver()
    {
        var resolver = Substitute.For<ITenantTemplateResolver>();
        resolver.ResolveListingTemplateFilter(Arg.Any<Guid?>()).Returns(Array.Empty<TemplateId>());
        return resolver;
    }

    internal static ITenantTemplateResolver CreateTemplateResolver(params TemplateId[] allowedTemplateIds)
    {
        var allowed = allowedTemplateIds.ToHashSet();
        var resolver = Substitute.For<ITenantTemplateResolver>();
        resolver.ResolveListingTemplateFilter(Arg.Any<Guid?>())
            .Returns(call =>
            {
                var requested = call.Arg<Guid?>();
                if (requested.HasValue)
                {
                    var templateId = new TemplateId(requested.Value);
                    return allowed.Contains(templateId)
                        ? new[] { templateId }
                        : Array.Empty<TemplateId>();
                }

                return allowed.ToList().AsReadOnly();
            });
        return resolver;
    }

    internal static void AttachTemplateVersion(
        Domain.Entities.Application application,
        TemplateId templateId,
        UserId createdBy)
    {
        var template = new Template(templateId, "Test Template", DateTime.UtcNow, createdBy);
        var templateVersion = new TemplateVersion(
            new TemplateVersionId(Guid.NewGuid()),
            templateId,
            "1.0",
            "{}",
            DateTime.UtcNow,
            createdBy);
        templateVersion.GetType().GetProperty(nameof(TemplateVersion.Template))?.SetValue(templateVersion, template);
        application.GetType().GetProperty(nameof(Domain.Entities.Application.TemplateVersion))
            ?.SetValue(application, templateVersion);
    }

    internal static void ConfigurePassthroughCache(
        ICacheService<IRedisCacheType> cache,
        string methodName)
    {
        cache.GetOrAddAsync(
                Arg.Any<string>(),
                Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(),
                methodName)
            .Returns(call => call.Arg<Func<Task<Result<PagedResult<ApplicationDto>>>>>()());
    }

    internal static GetApplicationsForUserQueryHandler CreateGetApplicationsForUserQueryHandler(
        IEaRepository<User> userRepo,
        IEaRepository<Domain.Entities.Application> appRepo,
        ITenantContextAccessor tenantContextAccessor,
        ITenantTemplateResolver templateResolver,
        ICacheService<IRedisCacheType>? cache = null)
    {
        cache ??= Substitute.For<ICacheService<IRedisCacheType>>();
        ConfigurePassthroughCache(cache, nameof(GetApplicationsForUserQueryHandler));

        return new GetApplicationsForUserQueryHandler(
            userRepo,
            appRepo,
            cache,
            tenantContextAccessor,
            templateResolver,
            Substitute.For<ILogger<GetApplicationsForUserQueryHandler>>());
    }

    internal static GetApplicationsForUserByExternalProviderIdQueryHandler CreateGetApplicationsForUserByExternalProviderIdQueryHandler(
        IEaRepository<User> userRepo,
        IEaRepository<Domain.Entities.Application> appRepo,
        ITenantTemplateResolver templateResolver,
        ICacheService<IRedisCacheType>? cache = null,
        ITenantContextAccessor? tenantContextAccessor = null)
    {
        cache ??= Substitute.For<ICacheService<IRedisCacheType>>();
        ConfigurePassthroughCache(cache, nameof(GetApplicationsForUserByExternalProviderIdQueryHandler));
        tenantContextAccessor ??= Substitute.For<ITenantContextAccessor>();

        return new GetApplicationsForUserByExternalProviderIdQueryHandler(
            userRepo,
            appRepo,
            cache,
            tenantContextAccessor,
            templateResolver);
    }
}
