using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Infrastructure.Services;

/// <summary>
/// Implementation of SignalR notification service
/// </summary>
public class NotificationSignalRService(
    INotificationHubContext hubContext,
    ITenantContextAccessor tenantContextAccessor,
    ILogger<NotificationSignalRService> logger)
    : INotificationSignalRService
{
    /// <summary>
    /// Gets the tenant-scoped group name for a user.
    /// Format: "tenant:{tenantId}:user:{userEmail}" or "user:{userEmail}" if no tenant context.
    /// </summary>
    private string GetUserGroupName(string userEmail)
    {
        var tenantId = tenantContextAccessor.CurrentTenant?.Id;
        return tenantId.HasValue 
            ? $"tenant:{tenantId}:user:{userEmail}" 
            : $"user:{userEmail}";
    }

    public async Task SendNotificationToUserAsync(string userEmail, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var groupName = GetUserGroupName(userEmail);
            await hubContext.SendToGroupAsync(groupName, "notification.upserted", new object?[] { notification }, cancellationToken);
            
            logger.LogDebug("Sent notification.upserted to group {GroupName} for notification {NotificationId}", 
                groupName, notification.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification.upserted to user {UserEmail} for notification {NotificationId}", 
                userEmail, notification.Id);
        }
    }

    public async Task SendNotificationUpdateToUserAsync(string userEmail, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            var groupName = GetUserGroupName(userEmail);
            await hubContext.SendToGroupAsync(groupName, "notification.updated", new object?[] { notification }, cancellationToken);
            
            logger.LogDebug("Sent notification.updated to group {GroupName} for notification {NotificationId}", 
                groupName, notification.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification.updated to user {UserEmail} for notification {NotificationId}", 
                userEmail, notification.Id);
        }
    }

    public async Task SendNotificationDeletionToUserAsync(string userEmail, string notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            var groupName = GetUserGroupName(userEmail);
            await hubContext.SendToGroupAsync(groupName, "notification.deleted", new object?[] { notificationId }, cancellationToken);
            
            logger.LogDebug("Sent notification.deleted to group {GroupName} for notification {NotificationId}", 
                groupName, notificationId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification.deleted to user {UserEmail} for notification {NotificationId}", 
                userEmail, notificationId);
        }
    }

    public async Task SendNotificationListRefreshToUserAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        try
        {
            var groupName = GetUserGroupName(userEmail);
            await hubContext.SendToGroupAsync(groupName, "notifications.refresh", new object?[] { }, cancellationToken);
            
            logger.LogDebug("Sent notifications.refresh to group {GroupName}", groupName);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notifications.refresh to user {UserEmail}", userEmail);
        }
    }
}
