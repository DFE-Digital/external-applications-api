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
using NSubstitute;
using Microsoft.Extensions.Logging;
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
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
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

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<PagedResult<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserQueryHandler(userRepo, appRepo, cache, tenantContextAccessor, Substitute.For<ILogger<GetApplicationsForUserQueryHandler>>());
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail, true), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(app.Id!.Value, result.Value!.Items.First().ApplicationId);
        Assert.NotNull(result.Value!.Items.First().TemplateSchema);
        Assert.Equal(template.Id!.Value, result.Value!.Items.First().TemplateSchema.TemplateId);
        Assert.Equal(templateVersion.Id!.Value, result.Value!.Items.First().TemplateSchema.TemplateVersionId);
        Assert.Equal("1.0", result.Value!.Items.First().TemplateSchema.VersionNumber);
        Assert.Equal("{}", result.Value!.Items.First().TemplateSchema.JsonSchema);
    }

    [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnApplicationsWithoutSchema_WhenIncludeSchemaIsFalse(
        string rawEmail,
        UserCustomization userCustom,
        PermissionCustomization permCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        userCustom.OverrideEmail = rawEmail;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var backing = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backing.SetValue(user, new List<Permission>());

        var template = new Template(new TemplateId(Guid.NewGuid()), "Test Template", DateTime.UtcNow, user.Id!);
        var templateVersion = new TemplateVersion(new TemplateVersionId(Guid.NewGuid()), template.Id!, "1.0", "{}", DateTime.UtcNow, user.Id!);
        templateVersion.GetType().GetProperty("Template")?.SetValue(templateVersion, template);

        var app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
        app.GetType().GetProperty("TemplateVersion")?.SetValue(app, templateVersion);

        var perm = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        ((List<Permission>)backing.GetValue(user)!).Add(perm);

        var userList = new List<User> { user };
        userRepo.Query().Returns(userList.AsQueryable().BuildMock());

        var appList = new List<Domain.Entities.Application> { app };
        appRepo.Query().Returns(appList.AsQueryable().BuildMock());

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<PagedResult<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserQueryHandler(userRepo, appRepo, cache, tenantContextAccessor, Substitute.For<ILogger<GetApplicationsForUserQueryHandler>>());
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(app.Id!.Value, result.Value!.Items.First().ApplicationId);
        Assert.Null(result.Value!.Items.First().TemplateSchema);
    }

    [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnApplicationsWithoutSchema_WhenIncludeSchemaIsDefaultFalse(
        string rawEmail,
        UserCustomization userCustom,
        PermissionCustomization permCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        userCustom.OverrideEmail = rawEmail;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var backing = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        backing.SetValue(user, new List<Permission>());

        var template = new Template(new TemplateId(Guid.NewGuid()), "Test Template", DateTime.UtcNow, user.Id!);
        var templateVersion = new TemplateVersion(new TemplateVersionId(Guid.NewGuid()), template.Id!, "1.0", "{}", DateTime.UtcNow, user.Id!);
        templateVersion.GetType().GetProperty("Template")?.SetValue(templateVersion, template);

        var app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
        app.GetType().GetProperty("TemplateVersion")?.SetValue(app, templateVersion);

        var perm = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        ((List<Permission>)backing.GetValue(user)!).Add(perm);

        var userList = new List<User> { user };
        userRepo.Query().Returns(userList.AsQueryable().BuildMock());

        var appList = new List<Domain.Entities.Application> { app };
        appRepo.Query().Returns(appList.AsQueryable().BuildMock());

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<PagedResult<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserQueryHandler(userRepo, appRepo, cache, tenantContextAccessor, Substitute.For<ILogger<GetApplicationsForUserQueryHandler>>());
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(app.Id!.Value, result.Value!.Items.First().ApplicationId);
        Assert.Null(result.Value!.Items.First().TemplateSchema);
    }

    [Theory, CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldReturnEmpty_WhenUserNotFound(
        string rawEmail,
        UserCustomization userCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        var userQ = new List<User>().AsQueryable().BuildMock();
        userRepo.Query().Returns(userQ);
        appRepo.Query().Returns(new List<Domain.Entities.Application>().AsQueryable().BuildMock());

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserQueryHandler))
            .Returns(call =>
            {
                var f = call.Arg<Func<Task<Result<PagedResult<ApplicationDto>>>>>();
                return f();
            });

        var handler = new GetApplicationsForUserQueryHandler(userRepo, appRepo, cache, tenantContextAccessor, Substitute.For<ILogger<GetApplicationsForUserQueryHandler>>());
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail, false), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("User not found", result.Error!);
    }

    [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnAllResults_WithDefaultPageMetadata_WhenNoPaginationParamsProvided(
        string rawEmail,
        UserCustomization userCustom,
        PermissionCustomization permCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        userCustom.OverrideEmail = rawEmail;
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

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserQueryHandler))
            .Returns(call => call.Arg<Func<Task<Result<PagedResult<ApplicationDto>>>>>()());

        var handler = new GetApplicationsForUserQueryHandler(userRepo, appRepo, cache, tenantContextAccessor, Substitute.For<ILogger<GetApplicationsForUserQueryHandler>>());
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Items.Count);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(1, result.Value!.PageNumber);
        Assert.Equal(2, result.Value!.PageSize);
        Assert.Equal(1, result.Value!.TotalPages);
    }

    [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnPagedResults_WhenPageNumberAndPageSizeProvided(
        string rawEmail,
        UserCustomization userCustom,
        PermissionCustomization permCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        userCustom.OverrideEmail = rawEmail;
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

        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserQueryHandler))
            .Returns(call => call.Arg<Func<Task<Result<PagedResult<ApplicationDto>>>>>()());

        var handler = new GetApplicationsForUserQueryHandler(userRepo, appRepo, cache, tenantContextAccessor, Substitute.For<ILogger<GetApplicationsForUserQueryHandler>>());
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail, false, null, PageNumber: 1, PageSize: 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(1, result.Value!.PageNumber);
        Assert.Equal(1, result.Value!.PageSize);
        Assert.Equal(2, result.Value!.TotalPages);
    }

    [Theory, CustomAutoData]
    public async Task Handle_ShouldReturnFromCache(
        string rawEmail,
        List<ApplicationDto> cached,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        var pagedResult = new PagedResult<ApplicationDto>
        {
            Items = cached.AsReadOnly(),
            TotalCount = cached.Count,
            PageNumber = 1,
            PageSize = cached.Count,
            TotalPages = 1
        };
        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), nameof(GetApplicationsForUserQueryHandler))
            .Returns(Task.FromResult(Result<PagedResult<ApplicationDto>>.Success(pagedResult)));

        var handler = new GetApplicationsForUserQueryHandler(userRepo, appRepo, cache, tenantContextAccessor, Substitute.For<ILogger<GetApplicationsForUserQueryHandler>>());
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail, false), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(cached.Count, result.Value!.TotalCount);
        userRepo.DidNotReceive().Query();
    }

    [Theory, CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenCacheThrows(
        string rawEmail,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        cache.GetOrAddAsync(Arg.Any<string>(), Arg.Any<Func<Task<Result<PagedResult<ApplicationDto>>>>>(), Arg.Any<string>())
            .Throws(new Exception("Boom"));

        var handler = new GetApplicationsForUserQueryHandler(userRepo, appRepo, cache, tenantContextAccessor, Substitute.For<ILogger<GetApplicationsForUserQueryHandler>>());
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail, false), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Boom", result.Error);
        userRepo.DidNotReceive().Query();
    }
}
