using AutoFixture;
using AutoFixture.Xunit2;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Application.Tests.Helpers;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Applications;

public class GetApplicationsForUserQueryHandlerTests
{
    [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnApplications_WhenUserHasPermissions(
        string rawEmail,
        UserCustomization userCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        userCustom.OverrideEmail = rawEmail;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

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

        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application> { app }.AsQueryable().BuildMock());

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserQueryHandler(
            userRepo,
            appRepo,
            tenantContextAccessor,
            ApplicationListingTestHelper.CreateTemplateResolver(template.Id!),
            permissionRepo: ApplicationListingTestHelper.CreatePermissionRepo(perm));
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
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        userCustom.OverrideEmail = rawEmail;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var template = new Template(new TemplateId(Guid.NewGuid()), "Test Template", DateTime.UtcNow, user.Id!);
        var templateVersion = new TemplateVersion(new TemplateVersionId(Guid.NewGuid()), template.Id!, "1.0", "{}", DateTime.UtcNow, user.Id!);
        templateVersion.GetType().GetProperty("Template")?.SetValue(templateVersion, template);

        var app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
        app.GetType().GetProperty("TemplateVersion")?.SetValue(app, templateVersion);

        var perm = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);

        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application> { app }.AsQueryable().BuildMock());

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserQueryHandler(
            userRepo,
            appRepo,
            tenantContextAccessor,
            ApplicationListingTestHelper.CreateTemplateResolver(template.Id!),
            permissionRepo: ApplicationListingTestHelper.CreatePermissionRepo(perm));
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
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        userCustom.OverrideEmail = rawEmail;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var template = new Template(new TemplateId(Guid.NewGuid()), "Test Template", DateTime.UtcNow, user.Id!);
        var templateVersion = new TemplateVersion(new TemplateVersionId(Guid.NewGuid()), template.Id!, "1.0", "{}", DateTime.UtcNow, user.Id!);
        templateVersion.GetType().GetProperty("Template")?.SetValue(templateVersion, template);

        var app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
        app.GetType().GetProperty("TemplateVersion")?.SetValue(app, templateVersion);

        var perm = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);

        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application> { app }.AsQueryable().BuildMock());

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserQueryHandler(
            userRepo,
            appRepo,
            tenantContextAccessor,
            ApplicationListingTestHelper.CreateTemplateResolver(template.Id!),
            permissionRepo: ApplicationListingTestHelper.CreatePermissionRepo(perm));
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(app.Id!.Value, result.Value!.Items.First().ApplicationId);
        Assert.Null(result.Value!.Items.First().TemplateSchema);
    }

    [Theory, CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldReturnEmpty_WhenUserNotFound(
        string rawEmail,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        userRepo.Query().Returns(new List<User>().AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application>().AsQueryable().BuildMock());

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserQueryHandler(
            userRepo,
            appRepo,
            tenantContextAccessor,
            ApplicationListingTestHelper.CreateTemplateResolver(new TemplateId(Guid.NewGuid())));
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail, false), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("User not found", result.Error!);
    }

    [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnAllResults_WithDefaultPageMetadata_WhenNoPaginationParamsProvided(
        string rawEmail,
        UserCustomization userCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        userCustom.OverrideEmail = rawEmail;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var templateId = new TemplateId(Guid.NewGuid());
        var appFixture = new Fixture().Customize(appCustom);
        var app1 = appFixture.Create<Domain.Entities.Application>();
        var app2 = appFixture.Create<Domain.Entities.Application>();
        ApplicationListingTestHelper.AttachTemplateVersion(app1, templateId, user.Id!);
        ApplicationListingTestHelper.AttachTemplateVersion(app2, templateId, user.Id!);

        var perm1 = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app1.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        var perm2 = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app2.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);

        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application> { app1, app2 }.AsQueryable().BuildMock());

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserQueryHandler(
            userRepo,
            appRepo,
            tenantContextAccessor,
            ApplicationListingTestHelper.CreateTemplateResolver(templateId),
            permissionRepo: ApplicationListingTestHelper.CreatePermissionRepo(perm1, perm2));
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
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        userCustom.OverrideEmail = rawEmail;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var templateId = new TemplateId(Guid.NewGuid());
        var appFixture = new Fixture().Customize(appCustom);
        var app1 = appFixture.Create<Domain.Entities.Application>();
        var app2 = appFixture.Create<Domain.Entities.Application>();
        ApplicationListingTestHelper.AttachTemplateVersion(app1, templateId, user.Id!);
        ApplicationListingTestHelper.AttachTemplateVersion(app2, templateId, user.Id!);

        var perm1 = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app1.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        var perm2 = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app2.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);

        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application> { app1, app2 }.AsQueryable().BuildMock());

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserQueryHandler(
            userRepo,
            appRepo,
            tenantContextAccessor,
            ApplicationListingTestHelper.CreateTemplateResolver(templateId),
            permissionRepo: ApplicationListingTestHelper.CreatePermissionRepo(perm1, perm2));
        var result = await handler.Handle(new GetApplicationsForUserQuery(rawEmail, false, null, PageNumber: 1, PageSize: 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(1, result.Value!.PageNumber);
        Assert.Equal(1, result.Value!.PageSize);
        Assert.Equal(2, result.Value!.TotalPages);
    }

    [Theory, CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFilteredResults_WhenSearchReferenceProvided(
        string rawEmail,
        UserCustomization userCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ICacheService<IRedisCacheType> cache,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        userCustom.OverrideEmail = rawEmail;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var templateId = new TemplateId(Guid.NewGuid());
        var matchCustom = new ApplicationCustomization { OverrideReference = "APP-2024-001" };
        var noMatchCustom = new ApplicationCustomization { OverrideReference = "XYZ-9999-999" };
        var matchApp = new Fixture().Customize(matchCustom).Create<Domain.Entities.Application>();
        var noMatchApp = new Fixture().Customize(noMatchCustom).Create<Domain.Entities.Application>();
        ApplicationListingTestHelper.AttachTemplateVersion(matchApp, templateId, user.Id!);
        ApplicationListingTestHelper.AttachTemplateVersion(noMatchApp, templateId, user.Id!);

        var perm1 = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, matchApp.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);
        var perm2 = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, noMatchApp.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);

        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application> { matchApp, noMatchApp }.AsQueryable().BuildMock());

        ApplicationListingTestHelper.ConfigurePassthroughCache(cache, nameof(GetApplicationsForUserQueryHandler));

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserQueryHandler(
            userRepo,
            appRepo,
            tenantContextAccessor,
            ApplicationListingTestHelper.CreateTemplateResolver(templateId),
            cache,
            ApplicationListingTestHelper.CreatePermissionRepo(perm1, perm2));
        var result = await handler.Handle(
            new GetApplicationsForUserQuery(rawEmail, Search: new ApplicationListingSearchCriteria(Reference: "APP-2024")),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal("APP-2024-001", result.Value!.Items.First().ApplicationReference);
    }

    [Theory, CustomAutoData]
    public async Task Handle_ShouldQueryUserOncePerRequest_WhenCachePassthrough(
        string rawEmail,
        UserCustomization userCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo,
        [Frozen] ITenantContextAccessor tenantContextAccessor)
    {
        userCustom.OverrideEmail = rawEmail;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application>().AsQueryable().BuildMock());

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserQueryHandler(
            userRepo,
            appRepo,
            tenantContextAccessor,
            ApplicationListingTestHelper.CreateEmptyTemplateResolver());

        await handler.Handle(new GetApplicationsForUserQuery(rawEmail, false), CancellationToken.None);
        await handler.Handle(new GetApplicationsForUserQuery(rawEmail, false), CancellationToken.None);

        userRepo.Received(2).Query();
    }
}
