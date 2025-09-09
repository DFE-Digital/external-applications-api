using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Domain.Services;

namespace DfE.ExternalApplications.Tests.Common.Mocks;

/// <summary>
/// Mock implementation of INotificationSignalRService for testing
/// </summary>
public class MockNotificationSignalRService : INotificationSignalRService
{
    public List<NotificationDto> SentNotifications { get; } = new();
    public List<string> DeletedNotificationIds { get; } = new();
    public List<string> UsersWithRefreshSent { get; } = new();
    public List<string> UsersWithUpdatesSent { get; } = new();

    public Task SendNotificationToUserAsync(string userEmail, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        SentNotifications.Add(notification);
        return Task.CompletedTask;
    }

    public Task SendNotificationUpdateToUserAsync(string userEmail, NotificationDto notification, CancellationToken cancellationToken = default)
    {
        UsersWithUpdatesSent.Add(userEmail);
        return Task.CompletedTask;
    }

    public Task SendNotificationDeletionToUserAsync(string userEmail, string notificationId, CancellationToken cancellationToken = default)
    {
        DeletedNotificationIds.Add(notificationId);
        return Task.CompletedTask;
    }

    public Task SendNotificationListRefreshToUserAsync(string userEmail, CancellationToken cancellationToken = default)
    {
        UsersWithRefreshSent.Add(userEmail);
        return Task.CompletedTask;
    }

    public void Reset()
    {
        SentNotifications.Clear();
        DeletedNotificationIds.Clear();
        UsersWithRefreshSent.Clear();
        UsersWithUpdatesSent.Clear();
    }
}
