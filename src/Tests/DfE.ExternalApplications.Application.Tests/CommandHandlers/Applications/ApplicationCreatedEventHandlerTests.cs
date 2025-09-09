using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.EventHandlers;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Applications;

public class ApplicationCreatedEventHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldCreateAllRequiredPermissions_WhenEventReceived(
        ApplicationId applicationId,
        UserId userId,
        DateTime createdOn,
        TemplateVersionId tvId,
        string applicationReference,
        ILogger<ApplicationCreatedEventHandler> logger,
        IEaRepository<Permission> permissionRepo)
    {
        // Arrange
        var @event = new ApplicationCreatedEvent(applicationId, applicationReference, tvId, userId, createdOn);
        var handler = new ApplicationCreatedEventHandler(logger, permissionRepo);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert - Should create 5 permissions total
        await permissionRepo.Received(5).AddAsync(Arg.Any<Permission>(), Arg.Any<CancellationToken>());

        // Application Read permission
        await permissionRepo.Received(1).AddAsync(
            Arg.Is<Permission>(p =>
                p.UserId == userId &&
                p.ApplicationId == applicationId &&
                p.ResourceKey == applicationId.Value.ToString() &&
                p.ResourceType == ResourceType.Application &&
                p.AccessType == AccessType.Read),
            Arg.Any<CancellationToken>());

        // Application Write permission
        await permissionRepo.Received(1).AddAsync(
            Arg.Is<Permission>(p =>
                p.UserId == userId &&
                p.ApplicationId == applicationId &&
                p.ResourceKey == applicationId.Value.ToString() &&
                p.ResourceType == ResourceType.Application &&
                p.AccessType == AccessType.Write),
            Arg.Any<CancellationToken>());

        // ApplicationFiles Read permission
        await permissionRepo.Received(1).AddAsync(
            Arg.Is<Permission>(p =>
                p.UserId == userId &&
                p.ApplicationId == applicationId &&
                p.ResourceKey == applicationId.Value.ToString() &&
                p.ResourceType == ResourceType.ApplicationFiles &&
                p.AccessType == AccessType.Read),
            Arg.Any<CancellationToken>());

        // ApplicationFiles Write permission
        await permissionRepo.Received(1).AddAsync(
            Arg.Is<Permission>(p =>
                p.UserId == userId &&
                p.ApplicationId == applicationId &&
                p.ResourceKey == applicationId.Value.ToString() &&
                p.ResourceType == ResourceType.ApplicationFiles &&
                p.AccessType == AccessType.Write),
            Arg.Any<CancellationToken>());

        // ApplicationFiles Delete permission
        await permissionRepo.Received(1).AddAsync(
            Arg.Is<Permission>(p =>
                p.UserId == userId &&
                p.ApplicationId == applicationId &&
                p.ResourceKey == applicationId.Value.ToString() &&
                p.ResourceType == ResourceType.ApplicationFiles &&
                p.AccessType == AccessType.Delete),
            Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldLogInformationAndError_WhenEventHandled(
        ApplicationId applicationId,
        UserId userId,
        DateTime createdOn,
        TemplateVersionId tvId,
        string applicationReference,
        ILogger<ApplicationCreatedEventHandler> logger,
        IEaRepository<Permission> permissionRepo)
    {
        // Arrange
        var @event = new ApplicationCreatedEvent(applicationId, applicationReference, tvId, userId, createdOn);
        var handler = new ApplicationCreatedEventHandler(logger, permissionRepo);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Handling event: ApplicationCreatedEvent")),
            null,
            Arg.Any<Func<object, Exception, string>>());

        logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Event handled successfully: ApplicationCreatedEvent")),
            null,
            Arg.Any<Func<object, Exception, string>>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldLogError_WhenExceptionOccurs(
        ApplicationId applicationId,
        UserId userId,
        DateTime createdOn,
        TemplateVersionId tvId,
        string applicationReference,
        ILogger<ApplicationCreatedEventHandler> logger,
        IEaRepository<Permission> permissionRepo)
    {
        // Arrange
        var @event = new ApplicationCreatedEvent(applicationId, applicationReference, tvId, userId, createdOn);
        var exception = new Exception("Test exception");
        permissionRepo.AddAsync(Arg.Any<Permission>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(exception);

        var handler = new ApplicationCreatedEventHandler(logger, permissionRepo);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<Exception>(() => handler.Handle(@event, CancellationToken.None));
        Assert.Same(exception, ex);

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString().Contains("Error handling event: ApplicationCreatedEvent")),
            Arg.Is<Exception>(e => e == exception),
            Arg.Any<Func<object, Exception, string>>());
    }
} 