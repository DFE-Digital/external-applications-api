using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Templates.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable.NSubstitute;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Templates;

public class GetLatestTemplateSchemaByExternalProviderIdQueryHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(TemplatePermissionCustomization), typeof(TemplateVersionCustomization))]
    public async Task Handle_ShouldReturnLatestSchema_WhenUserHasAccess(
        string externalId,
        TemplatePermissionCustomization tpCustom,
        TemplateVersionCustomization tvCustom,
        [Frozen] IEaRepository<TemplatePermission> accessRepo,
        [Frozen] IEaRepository<TemplateVersion> versionRepo,
        [Frozen] ICacheService<IMemoryCacheType> cacheService)
    {
        // Arrange
        var template = new Fixture().Customize(new TemplateCustomization()).Create<Template>();
        var user = new Fixture().Customize(new UserCustomization { OverrideExternalProviderId = externalId }).Create<User>();
        var version = new Fixture().Customize(tvCustom).Create<TemplateVersion>();
        
        // Set up template version
        version.GetType().GetProperty(nameof(TemplateVersion.Template))!.SetValue(version, template);
        version.GetType().GetProperty(nameof(TemplateVersion.TemplateId))!.SetValue(version, template.Id);

        // Set up template permission
        var permission = new TemplatePermission(
            new TemplatePermissionId(Guid.NewGuid()),
            user.Id!,
            template.Id,
            CoreLibs.Contracts.ExternalApplications.Enums.AccessType.Write,
            DateTime.UtcNow,
            user.Id!);

        permission.GetType().GetProperty(nameof(TemplatePermission.Template))!.SetValue(permission, template);
        permission.GetType().GetProperty(nameof(TemplatePermission.User))!.SetValue(permission, user);

        // Set up mock queryables
        var permissionQ = new List<TemplatePermission> { permission }.AsQueryable().BuildMockDbSet();
        var versionQ = new List<TemplateVersion> { version }.AsQueryable().BuildMockDbSet();

        accessRepo.Query().Returns(permissionQ);
        versionRepo.Query().Returns(versionQ);

        var cacheKey = $"TemplateSchema_{CacheKeyHelper.GenerateHashedCacheKey(template.Id.Value.ToString())}_{externalId}";
        cacheService
            .GetOrAddAsync(
                cacheKey,
                Arg.Any<Func<Task<Result<TemplateSchemaDto>>>>(),
                nameof(GetLatestTemplateSchemaByExternalProviderIdQueryHandler))
            .Returns(call =>
            {
                var factory = call.Arg<Func<Task<Result<TemplateSchemaDto>>>>();
                return factory();
            });

        var handler = new GetLatestTemplateSchemaByExternalProviderIdQueryHandler(
            accessRepo, versionRepo, cacheService);

        // Act
        var result = await handler.Handle(
            new GetLatestTemplateSchemaByExternalProviderIdQuery(template.Id.Value, externalId),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(version.Id!.Value, result.Value.TemplateVersionId);
        Assert.Equal(version.JsonSchema, result.Value.JsonSchema);
        Assert.Equal(version.TemplateId.Value, result.Value.TemplateId);
        Assert.Equal(version.VersionNumber, result.Value.VersionNumber);
    }

    [Theory]
    [CustomAutoData(typeof(TemplatePermissionCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserHasNoAccess(
        string externalId,
        Guid templateId,
        [Frozen] IEaRepository<TemplatePermission> accessRepo,
        [Frozen] IEaRepository<TemplateVersion> versionRepo,
        [Frozen] ICacheService<IMemoryCacheType> cacheService)
    {
        // Arrange
        var permissionQ = new List<TemplatePermission>().AsQueryable().BuildMockDbSet();
        accessRepo.Query().Returns(permissionQ);

        var cacheKey = $"TemplateSchema_{CacheKeyHelper.GenerateHashedCacheKey(templateId.ToString())}_{externalId}";
        cacheService
            .GetOrAddAsync(
                cacheKey,
                Arg.Any<Func<Task<Result<TemplateSchemaDto>>>>(),
                nameof(GetLatestTemplateSchemaByExternalProviderIdQueryHandler))
            .Returns(call =>
            {
                var factory = call.Arg<Func<Task<Result<TemplateSchemaDto>>>>();
                return factory();
            });

        var handler = new GetLatestTemplateSchemaByExternalProviderIdQueryHandler(
            accessRepo, versionRepo, cacheService);

        // Act
        var result = await handler.Handle(
            new GetLatestTemplateSchemaByExternalProviderIdQuery(templateId, externalId),
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Access denied", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(TemplatePermissionCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenNoVersionsExist(
        string externalId,
        TemplatePermissionCustomization tpCustom,
        [Frozen] IEaRepository<TemplatePermission> accessRepo,
        [Frozen] IEaRepository<TemplateVersion> versionRepo,
        [Frozen] ICacheService<IMemoryCacheType> cacheService)
    {
        // Arrange
        var template = new Fixture().Customize(new TemplateCustomization()).Create<Template>();
        var user = new Fixture().Customize(new UserCustomization { OverrideExternalProviderId = externalId }).Create<User>();

        // Set up template permission
        var permission = new TemplatePermission(
            new TemplatePermissionId(Guid.NewGuid()),
            user.Id!,
            template.Id,
            CoreLibs.Contracts.ExternalApplications.Enums.AccessType.Write,
            DateTime.UtcNow,
            user.Id!);

        permission.GetType().GetProperty(nameof(TemplatePermission.Template))!.SetValue(permission, template);
        permission.GetType().GetProperty(nameof(TemplatePermission.User))!.SetValue(permission, user);

        var permissionQ = new List<TemplatePermission> { permission }.AsQueryable().BuildMockDbSet();
        var versionQ = new List<TemplateVersion>().AsQueryable().BuildMockDbSet();

        accessRepo.Query().Returns(permissionQ);
        versionRepo.Query().Returns(versionQ);

        var cacheKey = $"TemplateSchema_{CacheKeyHelper.GenerateHashedCacheKey(template.Id.Value.ToString())}_{externalId}";
        cacheService
            .GetOrAddAsync(
                cacheKey,
                Arg.Any<Func<Task<Result<TemplateSchemaDto>>>>(),
                nameof(GetLatestTemplateSchemaByExternalProviderIdQueryHandler))
            .Returns(call =>
            {
                var factory = call.Arg<Func<Task<Result<TemplateSchemaDto>>>>();
                return factory();
            });

        var handler = new GetLatestTemplateSchemaByExternalProviderIdQueryHandler(
            accessRepo, versionRepo, cacheService);

        // Act
        var result = await handler.Handle(
            new GetLatestTemplateSchemaByExternalProviderIdQuery(template.Id.Value, externalId),
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Template version not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(TemplatePermissionCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenExceptionOccurs(
        string externalId,
        Guid templateId,
        Exception exception,
        [Frozen] IEaRepository<TemplatePermission> accessRepo,
        [Frozen] IEaRepository<TemplateVersion> versionRepo,
        [Frozen] ICacheService<IMemoryCacheType> cacheService)
    {
        // Arrange
        accessRepo.Query().Throws(exception);

        var cacheKey = $"TemplateSchema_{CacheKeyHelper.GenerateHashedCacheKey(templateId.ToString())}_{externalId}";
        cacheService
            .GetOrAddAsync(
                cacheKey,
                Arg.Any<Func<Task<Result<TemplateSchemaDto>>>>(),
                nameof(GetLatestTemplateSchemaByExternalProviderIdQueryHandler))
            .Returns(call =>
            {
                var factory = call.Arg<Func<Task<Result<TemplateSchemaDto>>>>();
                return factory();
            });

        var handler = new GetLatestTemplateSchemaByExternalProviderIdQueryHandler(
            accessRepo, versionRepo, cacheService);

        // Act
        var result = await handler.Handle(
            new GetLatestTemplateSchemaByExternalProviderIdQuery(templateId, externalId),
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(exception.ToString(), result.Error);
    }
} 