using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;

namespace DfE.ExternalApplications.Domain.Services;

/// <summary>
/// Service for sending real-time notifications via SignalR
/// </summary>
public interface INotificationSignalRService
{
    /// <summary>
    /// Sends a notification to a specific user
    /// </summary>
    /// <param name="userEmail">The user's email address</param>
    /// <param name="notification">The notification to send</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendNotificationToUserAsync(string userEmail, NotificationDto notification, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a notification update to a specific user
    /// </summary>
    /// <param name="userEmail">The user's email address</param>
    /// <param name="notification">The updated notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendNotificationUpdateToUserAsync(string userEmail, NotificationDto notification, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a notification deletion to a specific user
    /// </summary>
    /// <param name="userEmail">The user's email address</param>
    /// <param name="notificationId">The ID of the deleted notification</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendNotificationDeletionToUserAsync(string userEmail, string notificationId, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sends a notification list refresh to a specific user
    /// </summary>
    /// <param name="userEmail">The user's email address</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendNotificationListRefreshToUserAsync(string userEmail, CancellationToken cancellationToken = default);
}
