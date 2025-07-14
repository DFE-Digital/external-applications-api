using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Caching.Helpers;
using DfE.CoreLibs.Caching.Interfaces;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Applications;

public class GetApplicationsForUserQueryHandlerTests
{
    [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnApplications_WhenUserHasPermissions(
        string rawEmail,
        UserCustomization userCustom,
        PermissionCustomization permCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IMemoryCacheType> cache)
    {
        userCustom.OverrideEmail = rawEmail;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var backing = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backing.SetValue(user, new List<Permission>());

        var template = new Template(
            new TemplateId(Guid.NewGuid()),
            "Test Template",
            DateTime.UtcNow,
            user.Id!);

        var templateVersion = new TemplateVersion(
            new TemplateVersionId(Guid.NewGuid()),
            template.Id!,
            "1.0",
            "{}",
            DateTime.UtcNow,
            user.Id!);
        templateVersion.GetType().GetProperty("Template")?.SetValue(templateVersion, template);

        var app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
        app.GetType().GetProperty("TemplateVersion")?.SetValue(app, templateVersion);
        
        var perm = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        ((List<Permission>)backing.GetValue(user)!).Add(perm);

        var userList = new List<User> { user };
        userRepo.Query().Returns(userList.AsQueryable().BuildMock());

        var appList = new List<Domain.Entities.Application> { app };
        appRepo.Query().Returns(appList.AsQueryable().BuildMock());

        var cacheKey = $"Applications_ForUser_{CacheKeyHelper.GenerateHashedCacheKey(rawEmail)}";
        cache.GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>(), nameof(GetApplicationsForUserQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserQueryHandler(userRepo, appRepo, cache);
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(app.Id!.Value, result.Value!.First().ApplicationId);
        Assert.NotNull(result.Value!.First().TemplateSchema);
        Assert.Equal(template.Id!.Value, result.Value!.First().TemplateSchema.TemplateId);
        Assert.Equal(templateVersion.Id!.Value, result.Value!.First().TemplateSchema.TemplateVersionId);
        Assert.Equal("1.0", result.Value!.First().TemplateSchema.VersionNumber);
        Assert.Equal("{}", result.Value!.First().TemplateSchema.JsonSchema);
    }

    [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnApplicationsWithoutSchema_WhenIncludeSchemaIsFalse(
        string rawEmail,
        UserCustomization userCustom,
        PermissionCustomization permCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IMemoryCacheType> cache)
    {
        userCustom.OverrideEmail = rawEmail;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var backing = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backing.SetValue(user, new List<Permission>());

        var template = new Template(
            new TemplateId(Guid.NewGuid()),
            "Test Template",
            DateTime.UtcNow,
            user.Id!);

        var templateVersion = new TemplateVersion(
            new TemplateVersionId(Guid.NewGuid()),
            template.Id!,
            "1.0",
            "{}",
            DateTime.UtcNow,
            user.Id!);
        templateVersion.GetType().GetProperty("Template")?.SetValue(templateVersion, template);

        var app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
        app.GetType().GetProperty("TemplateVersion")?.SetValue(app, templateVersion);
        
        var perm = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        ((List<Permission>)backing.GetValue(user)!).Add(perm);

        var userList = new List<User> { user };
        userRepo.Query().Returns(userList.AsQueryable().BuildMock());

        var appList = new List<Domain.Entities.Application> { app };
        appRepo.Query().Returns(appList.AsQueryable().BuildMock());

        var cacheKey = $"Applications_ForUser_{CacheKeyHelper.GenerateHashedCacheKey(rawEmail)}";
        cache.GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>(), nameof(GetApplicationsForUserQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserQueryHandler(userRepo, appRepo, cache);
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(app.Id!.Value, result.Value!.First().ApplicationId);
        Assert.Null(result.Value!.First().TemplateSchema);
    }

    [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnApplicationsWithoutSchema_WhenIncludeSchemaIsDefaultFalse(
        string rawEmail,
        UserCustomization userCustom,
        PermissionCustomization permCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IMemoryCacheType> cache)
    {
        userCustom.OverrideEmail = rawEmail;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var backing = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backing.SetValue(user, new List<Permission>());

        var template = new Template(
            new TemplateId(Guid.NewGuid()),
            "Test Template",
            DateTime.UtcNow,
            user.Id!);

        var templateVersion = new TemplateVersion(
            new TemplateVersionId(Guid.NewGuid()),
            template.Id!,
            "1.0",
            "{}",
            DateTime.UtcNow,
            user.Id!);
        templateVersion.GetType().GetProperty("Template")?.SetValue(templateVersion, template);

        var app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
        app.GetType().GetProperty("TemplateVersion")?.SetValue(app, templateVersion);
        
        var perm = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        ((List<Permission>)backing.GetValue(user)!).Add(perm);

        var userList = new List<User> { user };
        userRepo.Query().Returns(userList.AsQueryable().BuildMock());

        var appList = new List<Domain.Entities.Application> { app };
        appRepo.Query().Returns(appList.AsQueryable().BuildMock());

        var cacheKey = $"Applications_ForUser_{CacheKeyHelper.GenerateHashedCacheKey(rawEmail)}";
        cache.GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>(), nameof(GetApplicationsForUserQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserQueryHandler(userRepo, appRepo, cache);
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!);
        Assert.Equal(app.Id!.Value, result.Value!.First().ApplicationId);
        Assert.Null(result.Value!.First().TemplateSchema);
    }

    [Theory, CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldReturnEmpty_WhenUserNotFound(
        string rawEmail,
        UserCustomization userCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IMemoryCacheType> cache)
    {
        // Return empty query result to simulate user not found
        var userQ = new List<User>().AsQueryable().BuildMock();
        userRepo.Query().Returns(userQ);
        appRepo.Query().Returns(new List<Domain.Entities.Application>().AsQueryable().BuildMock());

        var cacheKey = $"Applications_ForUser_{CacheKeyHelper.GenerateHashedCacheKey(rawEmail)}";
        cache.GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>(), nameof(GetApplicationsForUserQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserQueryHandler(userRepo, appRepo, cache);
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail, false), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("User not found", result.Error!);
    }

    [Theory, CustomAutoData]
    public async Task Handle_ShouldReturnFromCache(
        string rawEmail,
        List<ApplicationDto> cached,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IMemoryCacheType> cache)
    {
        var readOnly = cached.AsReadOnly();
        var cacheKey = $"Applications_ForUser_{CacheKeyHelper.GenerateHashedCacheKey(rawEmail)}";
        cache.GetOrAddAsync(cacheKey, Arg.Any<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>(), nameof(GetApplicationsForUserQueryHandler))
            .Returns(Task.FromResult(Result<IReadOnlyCollection<ApplicationDto>>.Success(readOnly)));

        var handler = new GetApplicationsForUserQueryHandler(userRepo, appRepo, cache);
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(readOnly.Count, result.Value!.Count);
        userRepo.DidNotReceive().Query();
    }

    [Theory, CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenCacheThrows(
        string rawEmail,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IMemoryCacheType> cache)
    {
        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<IReadOnlyCollection<ApplicationDto>>>>>(), Arg.Any<string>())
            .Throws(new Exception("Boom"));

        var handler = new GetApplicationsForUserQueryHandler(userRepo, appRepo, cache);
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail, false), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Boom", result.Error);
        userRepo.DidNotReceive().Query();
    }
}