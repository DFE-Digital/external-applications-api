using AutoFixture;
using AutoFixture.Xunit2;
using GovUK.Dfe.CoreLibs.Caching.Helpers;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;
using MockQueryable.NSubstitute;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Applications;

public class GetApplicationsForUserByExternalProviderIdQueryHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnApplications_WhenUserHasPermissions(
        string externalProviderId,
        UserCustomization userCustom,
        PermissionCustomization permCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        // Arrange
        userCustom.OverrideExternalProviderId = externalProviderId;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var backing = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backing.SetValue(user, new List<Permission>());

        var app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
        var perm = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        ((List<Permission>)backing.GetValue(user)!).Add(perm);

        var userList = new List<User> { user };
        userRepo.Query().Returns(userList.AsQueryable().BuildMock());

        var appList = new List<Domain.Entities.Application> { app };
        appRepo.Query().Returns(appList.AsQueryable().BuildMock());

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>(), nameof(GetApplicationsForUserByExternalProviderIdQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserByExternalProviderIdQueryHandler(userRepo, appRepo, cache, tenantContextAccessor);
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(app.Id!.Value, result.Value!.First().ApplicationId);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldReturnEmpty_WhenUserNotFound(
        string externalProviderId,
        UserCustomization userCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        // Arrange
        userCustom.OverrideExternalProviderId = "different-id";
        var user = new Fixture().Customize(userCustom).Create<User>();
        var userQ = new List<User> { user }.AsQueryable().BuildMock();
        userRepo.Query().Returns(userQ);
        appRepo.Query().Returns(new List<Domain.Entities.Application>().AsQueryable().BuildMock());

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>(), nameof(GetApplicationsForUserByExternalProviderIdQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserByExternalProviderIdQueryHandler(userRepo, appRepo, cache, tenantContextAccessor);
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnFromCache(
        string externalProviderId,
        List<ApplicationDto> cached,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        // Arrange
        var readOnly = cached.AsReadOnly();
        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>(), nameof(GetApplicationsForUserByExternalProviderIdQueryHandler))
            .Returns(Task.FromResult(Result<IReadOnlyCollection<ApplicationDto>>.Success(readOnly)));

        var handler = new GetApplicationsForUserByExternalProviderIdQueryHandler(userRepo, appRepo, cache, tenantContextAccessor);
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(readOnly.Count, result.Value!.Count);
        userRepo.DidNotReceive().Query();
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenCacheThrows(
        string externalProviderId,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        // Arrange
        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>(), Arg.Any<string>())
            .Throws(new Exception("Boom"));

        var handler = new GetApplicationsForUserByExternalProviderIdQueryHandler(userRepo, appRepo, cache, tenantContextAccessor);
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Boom", result.Error);
        userRepo.DidNotReceive().Query();
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenDatabaseExceptionOccurs(
        string externalProviderId,
        UserCustomization userCustom,
        PermissionCustomization permCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        // Arrange
        userCustom.OverrideExternalProviderId = externalProviderId;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var backing = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backing.SetValue(user, new List<Permission>());

        var app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
        var perm = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        ((List<Permission>)backing.GetValue(user)!).Add(perm);

        var userList = new List<User> { user };
        userRepo.Query().Returns(userList.AsQueryable().BuildMock());

        // Setup application repository to throw an exception
        appRepo.Query().Throws(new InvalidOperationException("Database connection failed"));

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>(), nameof(GetApplicationsForUserByExternalProviderIdQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserByExternalProviderIdQueryHandler(userRepo, appRepo, cache, tenantContextAccessor);
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Database connection failed", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldHandleApplicationWithNullTemplateVersion(
        string externalProviderId,
        UserCustomization userCustom,
        PermissionCustomization permCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        // Arrange
        userCustom.OverrideExternalProviderId = externalProviderId;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var backing = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backing.SetValue(user, new List<Permission>());

        // Create application with null template version (edge case)
        var app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
        
        // Use reflection to set TemplateVersion to null to test the null template name scenario
        var templateVersionField = typeof(Domain.Entities.Application).GetProperty("TemplateVersion");
        templateVersionField?.SetValue(app, null);

        var perm = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        ((List<Permission>)backing.GetValue(user)!).Add(perm);

        var userList = new List<User> { user };
        userRepo.Query().Returns(userList.AsQueryable().BuildMock());

        var appList = new List<Domain.Entities.Application> { app };
        appRepo.Query().Returns(appList.AsQueryable().BuildMock());

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>(), nameof(GetApplicationsForUserByExternalProviderIdQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserByExternalProviderIdQueryHandler(userRepo, appRepo, cache, tenantContextAccessor);
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(string.Empty, result.Value!.First().TemplateName); // Should handle null template gracefully
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldReturnEmpty_WhenUserHasNoApplicationPermissions(
        string externalProviderId,
        UserCustomization userCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        // Arrange
        userCustom.OverrideExternalProviderId = externalProviderId;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var backing = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backing.SetValue(user, new List<Permission>());

        // Add permission that doesn't have ApplicationId (template permission)
        var templatePerm = new Permission(
            new PermissionId(Guid.NewGuid()), 
            user.Id!, 
            null, // No ApplicationId
            "Template:Read", 
            ResourceType.Template, 
            AccessType.Read, 
            DateTime.UtcNow, 
            user.Id!);
        ((List<Permission>)backing.GetValue(user)!).Add(templatePerm);

        var userList = new List<User> { user };
        userRepo.Query().Returns(userList.AsQueryable().BuildMock());

        appRepo.Query().Returns(new List<Domain.Entities.Application>().AsQueryable().BuildMock());

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>(), nameof(GetApplicationsForUserByExternalProviderIdQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserByExternalProviderIdQueryHandler(userRepo, appRepo, cache, tenantContextAccessor);
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!); // Should return empty since no application permissions
    }
} 
