using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Application.Common.EventHandlers;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Application.Applications.EventHandlers;

public sealed class ContributorAddedEventHandler(
    ILogger<ContributorAddedEventHandler> logger,
    IEaRepository<User> userRepo) : BaseEventHandler<ContributorAddedEvent>(logger)
{
    protected override async Task HandleEvent(ContributorAddedEvent notification, CancellationToken cancellationToken)
    {
        // Load the user aggregate
        var user = await (new GetUserWithAllPermissionsByUserIdQueryObject(notification.ContributorId))
            .Apply(userRepo.Query())
            .FirstOrDefaultAsync(cancellationToken);

        if (user == null)
        {
            logger.LogWarning("User {UserId} not found when handling ContributorAddedEvent", notification.ContributorId.Value);
            return;
        }

        // Check if permissions already exist (idempotent check)
        var hasReadPermission = user.Permissions
            .Any(p => p.ApplicationId == notification.ApplicationId && 
                     p.ResourceType == ResourceType.Application && 
                     p.AccessType == AccessType.Read);

        var hasWritePermission = user.Permissions
            .Any(p => p.ApplicationId == notification.ApplicationId && 
                     p.ResourceType == ResourceType.Application && 
                     p.AccessType == AccessType.Write);

        // Add missing permissions using the aggregate's method
        if (!hasReadPermission)
        {
            user.AddPermission(
                notification.ApplicationId,
                notification.ApplicationId.Value.ToString(),
                ResourceType.Application,
                AccessType.Read,
                notification.AddedBy,
                notification.AddedOn);
        }

        if (!hasWritePermission)
        {
            user.AddPermission(
                notification.ApplicationId,
                notification.ApplicationId.Value.ToString(),
                ResourceType.Application,
                AccessType.Write,
                notification.AddedBy,
                notification.AddedOn);
        }

        // Note: The unit of work will be committed by the command handler that raised the event
        logger.LogInformation("Added permissions for contributor {ContributorId} to application {ApplicationId}", 
            notification.ContributorId.Value, notification.ApplicationId.Value);
    }
} 