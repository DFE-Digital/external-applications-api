using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
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
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldAddPermissions_WhenEventReceived(
        ApplicationId applicationId,
        TemplateId templateId,
        User contributor,
        UserId addedBy,
        DateTime addedOn,
        ILogger<ContributorAddedEventHandler> logger,
        IEaRepository<User> userRepo,
        IUserFactory userFactory)
    {
        // Arrange
        var @event = new ContributorAddedEvent(applicationId, templateId, contributor, addedBy, addedOn);
        var handler = new ContributorAddedEventHandler(logger, userRepo, userFactory);

        // Act
        await handler.Handle(@event, CancellationToken.None);

        // Assert
        // Verify that both application and template permissions were added
        userFactory.Received(1).AddPermissionToUser(
            contributor,
            applicationId.Value.ToString(),
            ResourceType.Application,
            Arg.Is<AccessType[]>(a => a.Length == 2 && a.Contains(AccessType.Read) && a.Contains(AccessType.Write)),
            addedBy,
            applicationId,
            addedOn);

        userFactory.Received(1).AddTemplatePermissionToUser(
            contributor,
            templateId.Value.ToString(),
            Arg.Is<AccessType[]>(a => a.Length == 2 && a.Contains(AccessType.Read) && a.Contains(AccessType.Write)),
            addedBy,
            addedOn);

        // Verify logging
        logger.Received(1).LogInformation(
            Arg.Is<string>(s => s.Contains("Added permissions for contributor")),
            Arg.Any<object[]>());
    }
} 