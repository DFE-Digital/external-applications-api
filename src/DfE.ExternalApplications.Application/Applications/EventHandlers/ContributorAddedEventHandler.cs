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
        // Add application permissions as side effect
        userFactory.AddPermissionToUser(
            notification.Contributor,
            notification.ApplicationId.Value.ToString(),
            ResourceType.Application,
            new[] { AccessType.Read, AccessType.Write },
            notification.AddedBy,
            notification.ApplicationId,
            notification.AddedOn);

        // Add template permissions as side effect
        userFactory.AddTemplatePermissionToUser(
            notification.Contributor,
            notification.TemplateId.Value.ToString(),
            new[] { AccessType.Read },
            notification.AddedBy,
            notification.AddedOn);

        userFactory.AddPermissionToUser(
            notification.Contributor,
            notification.ApplicationId.Value.ToString(),
            ResourceType.ApplicationFiles,
            new[] { AccessType.Read, AccessType.Write },
            notification.AddedBy,
            notification.ApplicationId,
            notification.AddedOn);

        logger.LogInformation("Added permissions for contributor {ContributorId} to application {ApplicationId} and template {TemplateId} by {AddedBy}", 
            notification.Contributor.Id!.Value, 
            notification.ApplicationId.Value, 
            notification.TemplateId.Value,
            notification.AddedBy.Value);
    }
} 