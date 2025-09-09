using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.EventHandlers;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Tests.EventHandlers;

public class ContributorAddedEventHandlerTests
{
    private readonly ILogger<ContributorAddedEventHandler> _logger;
    private readonly IEaRepository<User> _userRepo;
    private readonly IUserFactory _userFactory;
    private readonly ContributorAddedEventHandler _handler;

    public ContributorAddedEventHandlerTests()
    {
        _logger = Substitute.For<ILogger<ContributorAddedEventHandler>>();
        _userRepo = Substitute.For<IEaRepository<User>>();
        _userFactory = Substitute.For<IUserFactory>();

        _handler = new ContributorAddedEventHandler(_logger, _userRepo, _userFactory);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_Should_Add_Application_Permissions_To_Contributor(
        User contributor,
        ApplicationId applicationId,
        TemplateId templateId,
        UserId addedBy,
        DateTime addedOn)
    {
        // Arrange
        var @event = new ContributorAddedEvent(applicationId, templateId, contributor, addedBy, addedOn);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _userFactory.Received(1).AddPermissionToUser(
            contributor,
            applicationId.Value.ToString(),
            ResourceType.Application,
            Arg.Is<AccessType[]>(a => a.Length == 2 && a.Contains(AccessType.Read) && a.Contains(AccessType.Write)),
            addedBy,
            applicationId,
            addedOn);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_Should_Add_Template_Permissions_To_Contributor(
        User contributor,
        ApplicationId applicationId,
        TemplateId templateId,
        UserId addedBy,
        DateTime addedOn)
    {
        // Arrange
        var @event = new ContributorAddedEvent(applicationId, templateId, contributor, addedBy, addedOn);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _userFactory.Received(1).AddTemplatePermissionToUser(
            contributor,
            templateId.Value.ToString(),
            Arg.Is<AccessType[]>(a => a.Length == 1 && a.Contains(AccessType.Read)),
            addedBy,
            addedOn);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_Should_Add_Application_Files_Permissions_To_Contributor(
        User contributor,
        ApplicationId applicationId,
        TemplateId templateId,
        UserId addedBy,
        DateTime addedOn)
    {
        // Arrange
        var @event = new ContributorAddedEvent(applicationId, templateId, contributor, addedBy, addedOn);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _userFactory.Received(1).AddPermissionToUser(
            contributor,
            applicationId.Value.ToString(),
            ResourceType.ApplicationFiles,
            Arg.Is<AccessType[]>(a => a.Length == 2 && a.Contains(AccessType.Read) && a.Contains(AccessType.Write)),
            addedBy,
            applicationId,
            addedOn);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_Should_Log_Information_Message(
        User contributor,
        ApplicationId applicationId,
        TemplateId templateId,
        UserId addedBy,
        DateTime addedOn)
    {
        // Arrange
        var @event = new ContributorAddedEvent(applicationId, templateId, contributor, addedBy, addedOn);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Added permissions for contributor")),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_Should_Call_UserFactory_Methods_With_Correct_Parameters(
        User contributor,
        ApplicationId applicationId,
        TemplateId templateId,
        UserId addedBy,
        DateTime addedOn)
    {
        // Arrange
        var @event = new ContributorAddedEvent(applicationId, templateId, contributor, addedBy, addedOn);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _userFactory.Received(2).AddPermissionToUser(
            Arg.Any<User>(),
            Arg.Any<string>(),
            Arg.Any<ResourceType>(),
            Arg.Any<AccessType[]>(),
            Arg.Any<UserId>(),
            Arg.Any<ApplicationId>(),
            Arg.Any<DateTime>());

        _userFactory.Received(1).AddTemplatePermissionToUser(
            Arg.Any<User>(),
            Arg.Any<string>(),
            Arg.Any<AccessType[]>(),
            Arg.Any<UserId>(),
            Arg.Any<DateTime>());
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_Should_Not_Throw_Exception_When_UserFactory_Methods_Throw(
        User contributor,
        ApplicationId applicationId,
        TemplateId templateId,
        UserId addedBy,
        DateTime addedOn)
    {
        // Arrange
        var @event = new ContributorAddedEvent(applicationId, templateId, contributor, addedBy, addedOn);

        _userFactory.When(x => x.AddPermissionToUser(
            Arg.Any<User>(),
            Arg.Any<string>(),
            Arg.Any<ResourceType>(),
            Arg.Any<AccessType[]>(),
            Arg.Any<UserId>(),
            Arg.Any<ApplicationId>(),
            Arg.Any<DateTime>()))
            .Throw(new Exception("Test exception"));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<Exception>(() => _handler.Handle(@event, CancellationToken.None));
        Assert.Equal("Test exception", exception.Message);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_Should_Process_Event_With_Null_Contributor_Properties(
        User contributor,
        ApplicationId applicationId,
        TemplateId templateId,
        UserId addedBy,
        DateTime addedOn)
    {
        // Arrange
        var @event = new ContributorAddedEvent(applicationId, templateId, contributor, addedBy, addedOn);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _userFactory.Received(2).AddPermissionToUser(
            contributor,
            Arg.Any<string>(),
            Arg.Any<ResourceType>(),
            Arg.Any<AccessType[]>(),
            addedBy,
            applicationId,
            addedOn);

        _userFactory.Received(1).AddTemplatePermissionToUser(
            contributor,
            templateId.Value.ToString(),
            Arg.Any<AccessType[]>(),
            addedBy,
            addedOn);
    }
} 