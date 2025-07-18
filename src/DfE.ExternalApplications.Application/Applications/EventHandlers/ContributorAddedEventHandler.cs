using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Application.Common.EventHandlers;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Application.Applications.EventHandlers;

public sealed class ContributorAddedEventHandler(
    ILogger<ContributorAddedEventHandler> logger,
    IEaRepository<User> userRepo,
    IUserFactory userFactory) : BaseEventHandler<ContributorAddedEvent>(logger)
{
    protected override async Task HandleEvent(ContributorAddedEvent notification, CancellationToken cancellationToken)
    {
        // Add permissions to the contributor using the factory
        userFactory.AddPermissionToUser(
            notification.Contributor,
            notification.ApplicationId.Value.ToString(),
            ResourceType.Application,
            new[] { AccessType.Read, AccessType.Write },
            notification.AddedBy,
            notification.ApplicationId,
            notification.AddedOn);

        // Note: The unit of work will be committed by the command handler that raised the event
        logger.LogInformation("Added permissions for contributor {ContributorId} to application {ApplicationId}", 
            notification.Contributor.Id!.Value, notification.ApplicationId.Value);
    }
} 