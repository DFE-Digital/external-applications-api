using System.Collections.Concurrent;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Notifications.Interfaces;
using DfE.CoreLibs.Notifications.Models;

namespace DfE.ExternalApplications.Tests.Common.Helpers;

/// <summary>
/// Mock notification service that stores notifications in memory for testing purposes
/// </summary>
public class MockNotificationService : DfE.CoreLibs.Notifications.Interfaces.INotificationService
{
    private readonly ConcurrentDictionary<string, List<Notification>> _userNotifications = new();
    private int _notificationIdCounter = 1;

    public async Task AddSuccessAsync(string message, NotificationOptions? options = null, CancellationToken cancellationToken = default)
    {
        await AddNotificationAsync(message, NotificationType.Success, options, cancellationToken);
    }

    public async Task AddErrorAsync(string message, NotificationOptions? options = null, CancellationToken cancellationToken = default)
    {
        await AddNotificationAsync(message, NotificationType.Error, options, cancellationToken);
    }

    public async Task AddInfoAsync(string message, NotificationOptions? options = null, CancellationToken cancellationToken = default)
    {
        await AddNotificationAsync(message, NotificationType.Info, options, cancellationToken);
    }

    public async Task AddWarningAsync(string message, NotificationOptions? options = null, CancellationToken cancellationToken = default)
    {
        await AddNotificationAsync(message, NotificationType.Warning, options, cancellationToken);
    }

    public Task<Notification> AddNotificationAsync(string message, NotificationType type, NotificationOptions? options = null, CancellationToken cancellationToken = default)
    {
        var userId = options?.UserId ?? "default-user";
        var notificationId = _notificationIdCounter++.ToString();
        
        var notification = new Notification
        {
            Id = notificationId,
            Message = message,
            Type = type,
            Category = options?.Category,
            Context = options?.Context,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            AutoDismiss = options?.AutoDismiss ?? true,
            AutoDismissSeconds = options?.AutoDismissSeconds ?? 5,
            UserId = userId,
            ActionUrl = options?.ActionUrl,
            Metadata = options?.Metadata,
            Priority = options?.Priority ?? NotificationPriority.Normal
        };

        _userNotifications.AddOrUpdate(userId, 
            new List<Notification> { notification },
            (key, existing) => 
            {
                existing.Add(notification);
                return existing;
            });

        return Task.FromResult(notification);
    }

    public Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return Task.FromResult<IEnumerable<Notification>>(new List<Notification>());
        }

        if (_userNotifications.TryGetValue(userId, out var notifications))
        {
            var unreadNotifications = notifications.Where(n => !n.IsRead);
            return Task.FromResult<IEnumerable<Notification>>(unreadNotifications);
        }

        return Task.FromResult<IEnumerable<Notification>>(new List<Notification>());
    }

    public Task<IEnumerable<Notification>> GetAllNotificationsAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return Task.FromResult<IEnumerable<Notification>>(new List<Notification>());
        }

        if (_userNotifications.TryGetValue(userId, out var notifications))
        {
            return Task.FromResult<IEnumerable<Notification>>(notifications);
        }

        return Task.FromResult<IEnumerable<Notification>>(new List<Notification>());
    }

    public Task<IEnumerable<Notification>> GetNotificationsByCategoryAsync(string category, bool unreadOnly = false, string? userId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return Task.FromResult<IEnumerable<Notification>>(new List<Notification>());
        }

        if (_userNotifications.TryGetValue(userId, out var notifications))
        {
            var filteredNotifications = notifications.Where(n => n.Category == category);
            
            if (unreadOnly)
            {
                filteredNotifications = filteredNotifications.Where(n => !n.IsRead);
            }

            return Task.FromResult<IEnumerable<Notification>>(filteredNotifications);
        }

        return Task.FromResult<IEnumerable<Notification>>(new List<Notification>());
    }

    public Task MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        foreach (var userNotifications in _userNotifications.Values)
        {
            var notification = userNotifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                notification.IsRead = true;
                break;
            }
        }

        return Task.CompletedTask;
    }

    public Task MarkAllAsReadAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(userId) && _userNotifications.TryGetValue(userId, out var notifications))
        {
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
            }
        }

        return Task.CompletedTask;
    }

    public Task RemoveNotificationAsync(string notificationId, CancellationToken cancellationToken = default)
    {
        foreach (var userNotifications in _userNotifications.Values)
        {
            var notification = userNotifications.FirstOrDefault(n => n.Id == notificationId);
            if (notification != null)
            {
                userNotifications.Remove(notification);
                break;
            }
        }

        return Task.CompletedTask;
    }

    public Task ClearAllNotificationsAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(userId) && _userNotifications.TryGetValue(userId, out var notifications))
        {
            notifications.Clear();
        }

        return Task.CompletedTask;
    }

    public Task ClearNotificationsByCategoryAsync(string category, string? userId = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(userId) && _userNotifications.TryGetValue(userId, out var notifications))
        {
            var toRemove = notifications.Where(n => n.Category == category).ToList();
            foreach (var notification in toRemove)
            {
                notifications.Remove(notification);
            }
        }

        return Task.CompletedTask;
    }

    public Task ClearNotificationsByContextAsync(string context, string? userId = null, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrEmpty(userId) && _userNotifications.TryGetValue(userId, out var notifications))
        {
            var toRemove = notifications.Where(n => n.Context == context).ToList();
            foreach (var notification in toRemove)
            {
                notifications.Remove(notification);
            }
        }

        return Task.CompletedTask;
    }

    public Task<int> GetUnreadCountAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(userId))
        {
            return Task.FromResult(0);
        }

        if (_userNotifications.TryGetValue(userId, out var notifications))
        {
            var count = notifications.Count(n => !n.IsRead);
            return Task.FromResult(count);
        }

        return Task.FromResult(0);
    }
}