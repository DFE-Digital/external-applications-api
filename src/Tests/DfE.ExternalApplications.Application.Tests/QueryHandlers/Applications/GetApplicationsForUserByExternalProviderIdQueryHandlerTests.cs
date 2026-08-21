using AutoFixture;
using AutoFixture.Xunit2;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Application.Tests.Helpers;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MockQueryable;
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
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo)
    {
        userCustom.OverrideExternalProviderId = externalProviderId;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var templateId = new TemplateId(Guid.NewGuid());
        var app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
        ApplicationListingTestHelper.AttachTemplateVersion(app, templateId, user.Id!);
        var perm = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);

        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application> { app }.AsQueryable().BuildMock());

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserByExternalProviderIdQueryHandler(
            userRepo,
            appRepo,
            ApplicationListingTestHelper.CreateTemplateResolver(templateId),
            permissionRepo: ApplicationListingTestHelper.CreatePermissionRepo(perm));
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

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
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo)
    {
        userCustom.OverrideExternalProviderId = "different-id";
        var user = new Fixture().Customize(userCustom).Create<User>();
        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application>().AsQueryable().BuildMock());

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserByExternalProviderIdQueryHandler(
            userRepo,
            appRepo,
            ApplicationListingTestHelper.CreateEmptyTemplateResolver());
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnAllResults_WithDefaultPageMetadata_WhenNoPaginationParamsProvided(
        string externalProviderId,
        UserCustomization userCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo)
    {
        userCustom.OverrideExternalProviderId = externalProviderId;
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

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserByExternalProviderIdQueryHandler(
            userRepo,
            appRepo,
            ApplicationListingTestHelper.CreateTemplateResolver(templateId),
            permissionRepo: ApplicationListingTestHelper.CreatePermissionRepo(perm1, perm2));
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
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo)
    {
        userCustom.OverrideExternalProviderId = externalProviderId;
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

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserByExternalProviderIdQueryHandler(
            userRepo,
            appRepo,
            ApplicationListingTestHelper.CreateTemplateResolver(templateId),
            permissionRepo: ApplicationListingTestHelper.CreatePermissionRepo(perm1, perm2));
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId, false, null, PageNumber: 1, PageSize: 1), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Single(result.Value!.Items);
        Assert.Equal(2, result.Value!.TotalCount);
        Assert.Equal(1, result.Value!.PageNumber);
        Assert.Equal(1, result.Value!.PageSize);
        Assert.Equal(2, result.Value!.TotalPages);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldQueryDatabase_OnEveryRequest(
        string externalProviderId,
        UserCustomization userCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo)
    {
        userCustom.OverrideExternalProviderId = externalProviderId;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application>().AsQueryable().BuildMock());

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserByExternalProviderIdQueryHandler(
            userRepo,
            appRepo,
            ApplicationListingTestHelper.CreateEmptyTemplateResolver());

        await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);
        await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        userRepo.Received(2).Query();
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenDatabaseExceptionOccurs(
        string externalProviderId,
        UserCustomization userCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo)
    {
        userCustom.OverrideExternalProviderId = externalProviderId;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
        var perm = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);

        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Throws(new InvalidOperationException("Database connection failed"));

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserByExternalProviderIdQueryHandler(
            userRepo,
            appRepo,
            ApplicationListingTestHelper.CreateTemplateResolver(new TemplateId(Guid.NewGuid())),
            permissionRepo: ApplicationListingTestHelper.CreatePermissionRepo(perm));
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Database connection failed", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization), typeof(PermissionCustomization), typeof(ApplicationCustomization))]
    public async Task Handle_ShouldHandleApplicationWithNullTemplateVersion(
        string externalProviderId,
        UserCustomization userCustom,
        ApplicationCustomization appCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo)
    {
        userCustom.OverrideExternalProviderId = externalProviderId;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var app = new Fixture().Customize(appCustom).Create<Domain.Entities.Application>();
        typeof(Domain.Entities.Application).GetProperty("TemplateVersion")?.SetValue(app, null);

        var perm = new Permission(new PermissionId(Guid.NewGuid()), user.Id!, app.Id!, "Application:Read", ResourceType.Application, AccessType.Read, DateTime.UtcNow, user.Id!);

        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application> { app }.AsQueryable().BuildMock());

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserByExternalProviderIdQueryHandler(
            userRepo,
            appRepo,
            ApplicationListingTestHelper.CreateEmptyTemplateResolver(),
            permissionRepo: ApplicationListingTestHelper.CreatePermissionRepo(perm));
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldReturnEmpty_WhenUserHasNoApplicationPermissions(
        string externalProviderId,
        UserCustomization userCustom,
        [Frozen] IEaRepository<User> userRepo,
        [Frozen] IEaRepository<Domain.Entities.Application> appRepo)
    {
        userCustom.OverrideExternalProviderId = externalProviderId;
        userCustom.OverridePermissions = Array.Empty<Permission>();
        var fixture = new Fixture().Customize(userCustom);
        var user = fixture.Create<User>();

        var templatePerm = new Permission(
            new PermissionId(Guid.NewGuid()),
            user.Id!,
            null,
            "Template:Read",
            ResourceType.Template,
            AccessType.Read,
            DateTime.UtcNow,
            user.Id!);

        userRepo.Query().Returns(new List<User> { user }.AsQueryable().BuildMock());
        appRepo.Query().Returns(new List<Domain.Entities.Application>().AsQueryable().BuildMock());

        var handler = ApplicationListingTestHelper.CreateGetApplicationsForUserByExternalProviderIdQueryHandler(
            userRepo,
            appRepo,
            ApplicationListingTestHelper.CreateEmptyTemplateResolver(),
            permissionRepo: ApplicationListingTestHelper.CreatePermissionRepo(templatePerm));
        var result = await handler.Handle(new GetApplicationsForUserByExternalProviderIdQuery(externalProviderId), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Items);
    }
}
