using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Domain.Services;
using Microsoft.Extensions.Logging;

namespace DfE.ExternalApplications.Infrastructure.Services;

/// <summary>
/// Implementation of SignalR notification service
/// </summary>
public class NotificationSignalRService : INotificationSignalRService
{
    private readonly INotificationHubContext _hubContext;
    private readonly ILogger<NotificationSignalRService> _logger;

    public NotificationSignalRService(
        INotificationHubContext hubContext,
        ILogger<NotificationSignalRService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task SendNotificationToUserAsync(string userEmail, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.SendToGroupAsync($"user:{userEmail}", "notification.upserted", new object?[] { notification }, cancellationToken);
            
            _logger.LogDebug("Sent notification.upserted to user {UserEmail} for notification {NotificationId}", 
                userEmail, notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification.upserted to user {UserEmail} for notification {NotificationId}", 
                userEmail, notification.Id);
        }
    }

    public async Task SendNotificationUpdateToUserAsync(string userEmail, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.SendToGroupAsync($"user:{userEmail}", "notification.updated", new object?[] { notification }, cancellationToken);
            
            _logger.LogDebug("Sent notification.updated to user {UserEmail} for notification {NotificationId}", 
                userEmail, notification.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification.updated to user {UserEmail} for notification {NotificationId}", 
                userEmail, notification.Id);
        }
    }

    public async Task SendNotificationDeletionToUserAsync(string userEmail, string notificationId, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.SendToGroupAsync($"user:{userEmail}", "notification.deleted", new object?[] { notificationId }, cancellationToken);
            
            _logger.LogDebug("Sent notification.deleted to user {UserEmail} for notification {NotificationId}", 
                userEmail, notificationId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notification.deleted to user {UserEmail} for notification {NotificationId}", 
                userEmail, notificationId);
        }
    }

    public async Task SendNotificationListRefreshToUserAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.SendToGroupAsync($"user:{userEmail}", "notifications.refresh", new object?[] { }, cancellationToken);
            
            _logger.LogDebug("Sent notifications.refresh to user {UserEmail}", userEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send notifications.refresh to user {UserEmail}", userEmail);
        }
    }
}
