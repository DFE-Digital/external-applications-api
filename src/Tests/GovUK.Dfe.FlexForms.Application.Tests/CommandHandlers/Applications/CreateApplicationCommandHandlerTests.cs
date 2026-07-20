using AutoFixture.Xunit2;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Application.Applications.Commands;
using GovUK.Dfe.FlexForms.Application.Services;
using GovUK.Dfe.FlexForms.Application.Templates.Queries;
using GovUK.Dfe.FlexForms.Application.Tests.Helpers;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.Interfaces;
using GovUK.Dfe.FlexForms.Domain.Interfaces.Repositories;
using GovUK.Dfe.FlexForms.Domain.Services;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;
using MediatR;
using NSubstitute;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Application.Tests.CommandHandlers.Applications;

public class CreateApplicationCommandHandlerTests
{
    private static ITenantTemplateResolver AllowAllTenantTemplates()
    {
        var resolver = Substitute.For<ITenantTemplateResolver>();
        resolver.IsTemplateInCurrentTenantAsync(Arg.Any<TemplateId>(), Arg.Any<CancellationToken>())
            .Returns(true);
        return resolver;
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldCreateApplicationAndResponse_WhenValidRequestWithAppId(
        CreateApplicationCommand command,
        TemplateSchemaDto templateSchema,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Test User",
            "test@example.com",
            DateTime.UtcNow,
            null,
            null,
            null,
            "external-id");

        permissionCheckerService.HasPermission(ResourceType.Template, command.TemplateId.ToString(), AccessType.Write).Returns(true);
        permissionCheckerService.IsAdmin().Returns(true);

        var application = new Domain.Entities.Application(
            new ApplicationId(Guid.NewGuid()),
            "APP-001",
            new TemplateVersionId(templateSchema.TemplateVersionId),
            DateTime.UtcNow,
            user.Id!);

        mediator.Send(Arg.Any<GetLatestTemplateSchemaByUserIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TemplateSchemaDto>.Success(templateSchema));

        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            Substitute.For<IEaRepository<Template>>(),
            AuthenticatedUserServiceTestHelper.MockReturningUser(user),
            ApplicationCreationServiceTestHelper.MockReturning(application),
            permissionCheckerService,
            AllowAllTenantTemplates(),
            mediator,
            Substitute.For<IUserCacheInvalidator>(),
            unitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("APP-001", result.Value.ApplicationReference);
        Assert.Equal(templateSchema.TemplateVersionId, result.Value.TemplateVersionId);
        Assert.NotNull(result.Value.TemplateSchema);

        await applicationRepo.Received(1).AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldCreateApplicationAndResponse_WhenValidRequestWithEmail(
        CreateApplicationCommand command,
        TemplateSchemaDto templateSchema,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Test User",
            "test@example.com",
            DateTime.UtcNow,
            null,
            null,
            null);

        permissionCheckerService.HasPermission(ResourceType.Template, command.TemplateId.ToString(), AccessType.Write).Returns(true);
        permissionCheckerService.IsAdmin().Returns(true);

        var application = new Domain.Entities.Application(
            new ApplicationId(Guid.NewGuid()),
            "APP-001",
            new TemplateVersionId(templateSchema.TemplateVersionId),
            DateTime.UtcNow,
            user.Id!);

        mediator.Send(Arg.Any<GetLatestTemplateSchemaByUserIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TemplateSchemaDto>.Success(templateSchema));

        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            Substitute.For<IEaRepository<Template>>(),
            AuthenticatedUserServiceTestHelper.MockReturningUser(user),
            ApplicationCreationServiceTestHelper.MockReturning(application),
            permissionCheckerService,
            AllowAllTenantTemplates(),
            mediator,
            Substitute.For<IUserCacheInvalidator>(),
            unitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal("APP-001", result.Value.ApplicationReference);
        await applicationRepo.Received(1).AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotAuthenticated(
        CreateApplicationCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            Substitute.For<IEaRepository<Template>>(),
            AuthenticatedUserServiceTestHelper.MockReturning(Result<User>.Forbid("Not authenticated")),
            Substitute.For<IApplicationCreationService>(),
            permissionCheckerService,
            AllowAllTenantTemplates(),
            mediator,
            Substitute.For<IUserCacheInvalidator>(),
            unitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);
        await applicationRepo.DidNotReceive().AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenNoUserIdentifier(
        CreateApplicationCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            Substitute.For<IEaRepository<Template>>(),
            AuthenticatedUserServiceTestHelper.MockReturning(Result<User>.Forbid("No user identifier")),
            Substitute.For<IApplicationCreationService>(),
            permissionCheckerService,
            AllowAllTenantTemplates(),
            mediator,
            Substitute.For<IUserCacheInvalidator>(),
            unitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("No user identifier", result.Error);
        await applicationRepo.DidNotReceive().AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound(
        CreateApplicationCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            Substitute.For<IEaRepository<Template>>(),
            AuthenticatedUserServiceTestHelper.MockReturning(Result<User>.NotFound("User not found")),
            Substitute.For<IApplicationCreationService>(),
            permissionCheckerService,
            AllowAllTenantTemplates(),
            mediator,
            Substitute.For<IUserCacheInvalidator>(),
            unitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);
        await applicationRepo.DidNotReceive().AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserDoesNotHavePermission(
        CreateApplicationCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Test User",
            "test@example.com",
            DateTime.UtcNow,
            null,
            null,
            null,
            "external-id");

        permissionCheckerService.HasPermission(ResourceType.Template, command.TemplateId.ToString(), AccessType.Write).Returns(false);
        permissionCheckerService.IsAdmin().Returns(true);

        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            Substitute.For<IEaRepository<Template>>(),
            AuthenticatedUserServiceTestHelper.MockReturningUser(user),
            Substitute.For<IApplicationCreationService>(),
            permissionCheckerService,
            AllowAllTenantTemplates(),
            mediator,
            Substitute.For<IUserCacheInvalidator>(),
            unitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to create applications for this template", result.Error);
        await applicationRepo.DidNotReceive().AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenTemplateSchemaNotFound(
        CreateApplicationCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Test User",
            "test@example.com",
            DateTime.UtcNow,
            null,
            null,
            null,
            "external-id");

        permissionCheckerService.HasPermission(ResourceType.Template, command.TemplateId.ToString(), AccessType.Write).Returns(true);
        permissionCheckerService.IsAdmin().Returns(true);
        mediator.Send(Arg.Any<GetLatestTemplateSchemaByUserIdQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TemplateSchemaDto>.Failure("Template schema not found"));

        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            Substitute.For<IEaRepository<Template>>(),
            AuthenticatedUserServiceTestHelper.MockReturningUser(user),
            Substitute.For<IApplicationCreationService>(),
            permissionCheckerService,
            AllowAllTenantTemplates(),
            mediator,
            Substitute.For<IUserCacheInvalidator>(),
            unitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Template schema not found", result.Error);
        await applicationRepo.DidNotReceive().AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenExceptionThrown(
        CreateApplicationCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IPermissionCheckerService permissionCheckerService,
        ISender mediator,
        IUnitOfWork unitOfWork)
    {
        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Test User",
            "test@example.com",
            DateTime.UtcNow,
            null,
            null,
            null,
            "external-id");

        permissionCheckerService.HasPermission(ResourceType.Template, command.TemplateId.ToString(), AccessType.Write).Returns(true);
        permissionCheckerService.IsAdmin().Returns(true);
        mediator.When(x => x.Send(Arg.Any<GetLatestTemplateSchemaByUserIdQuery>(), Arg.Any<CancellationToken>()))
            .Do(_ => throw new InvalidOperationException("Database connection failed"));

        var handler = new CreateApplicationCommandHandler(
            applicationRepo,
            Substitute.For<IEaRepository<Template>>(),
            AuthenticatedUserServiceTestHelper.MockReturningUser(user),
            Substitute.For<IApplicationCreationService>(),
            permissionCheckerService,
            AllowAllTenantTemplates(),
            mediator,
            Substitute.For<IUserCacheInvalidator>(),
            unitOfWork);

        var result = await handler.Handle(command, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("Database connection failed", result.Error);
        await applicationRepo.DidNotReceive().AddAsync(Arg.Any<Domain.Entities.Application>(), Arg.Any<CancellationToken>());
    }
}
