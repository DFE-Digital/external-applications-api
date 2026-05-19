using AutoFixture;
using AutoFixture.Xunit2;
using GovUK.Dfe.CoreLibs.Caching.Helpers;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Application.Common.Models;
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

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserByExternalProviderIdQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<PagedResult<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserByExternalProviderIdQueryHandler(userRepo, appRepo, cache, tenantContextAccessor);
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(app.Id!.Value, result.Value!.Items.First().ApplicationId);
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

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserByExternalProviderIdQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<PagedResult<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserByExternalProviderIdQueryHandler(userRepo, appRepo, cache, tenantContextAccessor);
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnAllResults_WithDefaultPageMetadata_WhenNoPaginationParamsProvided(
        string externalProviderId,
        UserCustomization userCustom,
        PermissionCustomization permCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        userCustom.OverrideExternalProviderId = externalProviderId;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var backing = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backing.SetValue(user, new List<Permission>());

        var appFixture = new Fixture().Customize(appCustom);
        var app1 = appFixture.Create<Domain.Entities.Application>();
        var app2 = appFixture.Create<Domain.Entities.Application>();

        var perm1 = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app1.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        var perm2 = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app2.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        ((List<Permission>)backing.GetValue(user)!).Add(perm1);
        ((List<Permission>)backing.GetValue(user)!).Add(perm2);

        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application> { app1, app2 }.AsQueryable().BuildMock());

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserByExternalProviderIdQueryHandler))
            .Returns(call => call.Arg<Func<Task<Result<PagedResult<ApplicationDto>>>>>()());

        var handler = new GetApplicationsForUserByExternalProviderIdQueryHandler(userRepo, appRepo, cache, tenantContextAccessor);
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(1, result.Value!.PageNumber);
        Assert.Equal(2, result.Value!.PageSize);
        Assert.Equal(1, result.Value!.TotalPages);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnPagedResults_WhenPageNumberAndPageSizeProvided(
        string externalProviderId,
        UserCustomization userCustom,
        PermissionCustomization permCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        userCustom.OverrideExternalProviderId = externalProviderId;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var backing = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backing.SetValue(user, new List<Permission>());

        var appFixture = new Fixture().Customize(appCustom);
        var app1 = appFixture.Create<Domain.Entities.Application>();
        var app2 = appFixture.Create<Domain.Entities.Application>();

        var perm1 = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app1.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        var perm2 = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app2.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        ((List<Permission>)backing.GetValue(user)!).Add(perm1);
        ((List<Permission>)backing.GetValue(user)!).Add(perm2);

        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application> { app1, app2 }.AsQueryable().BuildMock());

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserByExternalProviderIdQueryHandler))
            .Returns(call => call.Arg<Func<Task<Result<PagedResult<ApplicationDto>>>>>()());

        var handler = new GetApplicationsForUserByExternalProviderIdQueryHandler(userRepo, appRepo, cache, tenantContextAccessor);
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId, false, null, PageNumber: 1, PageSize: 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(1, result.Value!.PageNumber);
        Assert.Equal(1, result.Value!.PageSize);
        Assert.Equal(2, result.Value!.TotalPages);
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
        var pagedResult = new PagedResult<ApplicationDto>
        {
            Items = cached.AsReadOnly(),
            TotalCount = cached.Count,
            PageNumber = 1,
            PageSize = cached.Count,
            TotalPages = 1
        };
        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserByExternalProviderIdQueryHandler))
            .Returns(Task.FromResult(Result<PagedResult<ApplicationDto>>.Success(pagedResult)));

        var handler = new GetApplicationsForUserByExternalProviderIdQueryHandler(userRepo, appRepo, cache, tenantContextAccessor);
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(cached.Count, result.Value!.TotalCount);
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
        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), Arg.Any<string>())
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

        appRepo.Query().Throws(new InvalidOperationException("Database connection failed"));

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserByExternalProviderIdQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<PagedResult<ApplicationDto>>>>>();
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

        var app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
        var templateVersionField = typeof(Domain.Entities.Application).GetProperty("TemplateVersion");
        templateVersionField?.SetValue(app, null);

        var perm = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        ((List<Permission>)backing.GetValue(user)!).Add(perm);

        var userList = new List<User> { user };
        userRepo.Query().Returns(userList.AsQueryable().BuildMock());

        var appList = new List<Domain.Entities.Application> { app };
        appRepo.Query().Returns(appList.AsQueryable().BuildMock());

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserByExternalProviderIdQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<PagedResult<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserByExternalProviderIdQueryHandler(userRepo, appRepo, cache, tenantContextAccessor);
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(string.Empty, result.Value!.Items.First().TemplateName);
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

        var templatePerm = new Permission(
            new PermissionId(Guid.NewGuid()),
            user.Id!,
            null,
            "Template:Read",
            ResourceType.Template,
            AccessType.Read,
            DateTime.UtcNow,
            user.Id!);
        ((List<Permission>)backing.GetValue(user)!).Add(templatePerm);

        var userList = new List<User> { user };
        userRepo.Query().Returns(userList.AsQueryable().BuildMock());

        appRepo.Query().Returns(new List<Domain.Entities.Application>().AsQueryable().BuildMock());

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserByExternalProviderIdQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<PagedResult<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserByExternalProviderIdQueryHandler(userRepo, appRepo, cache, tenantContextAccessor);
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }
}
