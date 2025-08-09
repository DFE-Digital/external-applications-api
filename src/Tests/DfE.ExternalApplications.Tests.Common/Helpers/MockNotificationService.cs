using System.Collections.Concurrent;

// TODO: Replace these using statements with the actual notification service package imports
// For example: using YourNotificationPackage.Models;
// using YourNotificationPackage.Services;

namespace DfE.ExternalApplications.Tests.Common.Helpers;

/// <summary>
/// Mock notification service that stores notifications in memory for testing purposes
/// </summary>
public class MockNotificationService : INotificationService
{
    private readonly ConcurrentDictionary<string, List<Notification>> _userNotifications = new();
    private int _notificationIdCounter = 1;

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
        userId ??= "default-user";
        
        if (!_userNotifications.TryGetValue(userId, out var notifications))
            return Task.FromResult(Enumerable.Empty<Notification>());

        var unread = notifications.Where(n => !n.IsRead);
        return Task.FromResult(unread);
    }

    public Task<IEnumerable<Notification>> GetAllNotificationsAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        userId ??= "default-user";
        
        if (!_userNotifications.TryGetValue(userId, out var notifications))
            return Task.FromResult(Enumerable.Empty<Notification>());

        return Task.FromResult(notifications.AsEnumerable());
    }

    public Task<IEnumerable<Notification>> GetNotificationsByCategoryAsync(string category, bool unreadOnly = false, string? userId = null, CancellationToken cancellationToken = default)
    {
        userId ??= "default-user";
        
        if (!_userNotifications.TryGetValue(userId, out var notifications))
            return Task.FromResult(Enumerable.Empty<Notification>());

        var filtered = notifications.Where(n => n.Category == category);
        
        if (unreadOnly)
            filtered = filtered.Where(n => !n.IsRead);

        return Task.FromResult(filtered);
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
        userId ??= "default-user";
        
        if (_userNotifications.TryGetValue(userId, out var notifications))
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
        userId ??= "default-user";
        
        if (_userNotifications.TryGetValue(userId, out var notifications))
        {
            notifications.Clear();
        }

        return Task.CompletedTask;
    }

    public Task ClearNotificationsByCategoryAsync(string category, string? userId = null, CancellationToken cancellationToken = default)
    {
        userId ??= "default-user";
        
        if (_userNotifications.TryGetValue(userId, out var notifications))
        {
            notifications.RemoveAll(n => n.Category == category);
        }

        return Task.CompletedTask;
    }

    public Task ClearNotificationsByContextAsync(string context, string? userId = null, CancellationToken cancellationToken = default)
    {
        userId ??= "default-user";
        
        if (_userNotifications.TryGetValue(userId, out var notifications))
        {
            notifications.RemoveAll(n => n.Context == context);
        }

        return Task.CompletedTask;
    }

    public Task<int> GetUnreadCountAsync(string? userId = null, CancellationToken cancellationToken = default)
    {
        userId ??= "default-user";
        
        if (!_userNotifications.TryGetValue(userId, out var notifications))
            return Task.FromResult(0);

        var count = notifications.Count(n => !n.IsRead);
        return Task.FromResult(count);
    }
}

// These models would normally come from your notification service NuGet package
// Temporarily including them here for compilation, but they should be imported from the package

public class Notification
{
    /// <summary>
    /// Unique identifier for the notification
    /// </summary>
    public string Id { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// The notification message
    /// </summary>
    public string Message { get; set; } = string.Empty;

    /// <summary>
    /// Type of notification (success, error, info, warning)
    /// </summary>
    public NotificationType Type { get; set; }

    /// <summary>
    /// When the notification was created
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Whether the notification has been seen/read
    /// </summary>
    public bool IsRead { get; set; } = false;

    /// <summary>
    /// Whether the notification should auto-dismiss after a timeout
    /// </summary>
    public bool AutoDismiss { get; set; } = true;

    /// <summary>
    /// Auto-dismiss timeout in seconds (default 5 seconds)
    /// </summary>
    public int AutoDismissSeconds { get; set; } = 5;

    /// <summary>
    /// Optional context information (e.g., fieldId, uploadId, etc.)
    /// Used for preventing duplicates and contextual operations
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Optional category for grouping notifications
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Optional user identifier for multi-user scenarios
    /// When null, applies to current session/user
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Optional action URL for notifications that link to specific resources
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Optional metadata for extensibility
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Priority level for notification ordering and display
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;
}

public class NotificationOptions
{
    /// <summary>
    /// Optional context information (e.g., fieldId, uploadId, etc.)
    /// Used for preventing duplicates and contextual operations
    /// </summary>
    public string? Context { get; set; }

    /// <summary>
    /// Optional category for grouping notifications
    /// </summary>
    public string? Category { get; set; }

    /// <summary>
    /// Whether the notification should auto-dismiss after a timeout
    /// </summary>
    public bool AutoDismiss { get; set; } = true;

    /// <summary>
    /// Auto-dismiss timeout in seconds
    /// </summary>
    public int AutoDismissSeconds { get; set; } = 5;

    /// <summary>
    /// Optional user identifier for multi-user scenarios
    /// When null, applies to current session/user
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Optional action URL for notifications that link to specific resources
    /// </summary>
    public string? ActionUrl { get; set; }

    /// <summary>
    /// Optional metadata for extensibility
    /// </summary>
    public Dictionary<string, object>? Metadata { get; set; }

    /// <summary>
    /// Priority level for notification ordering and display
    /// </summary>
    public NotificationPriority Priority { get; set; } = NotificationPriority.Normal;

    /// <summary>
    /// Whether to remove existing notifications with the same context before adding this one
    /// </summary>
    public bool ReplaceExistingContext { get; set; } = true;
}

public enum NotificationPriority
{
    /// <summary>
    /// Low priority - background information
    /// </summary>
    Low = 0,

    /// <summary>
    /// Normal priority - standard notifications
    /// </summary>
    Normal = 1,

    /// <summary>
    /// High priority - important notifications that should be prominent
    /// </summary>
    High = 2,

    /// <summary>
    /// Critical priority - urgent notifications requiring immediate attention
    /// </summary>
    Critical = 3
}

public enum NotificationType
{
    /// <summary>
    /// Success notification (green) - indicates successful operations
    /// </summary>
    Success,

    /// <summary>
    /// Error notification (red) - indicates failures or critical issues
    /// </summary>
    Error,

    /// <summary>
    /// Information notification (blue) - provides general information
    /// </summary>
    Info,

    /// <summary>
    /// Warning notification (yellow/amber) - indicates potential issues
    /// </summary>
    Warning
}

public interface INotificationService
{
    Task<Notification> AddNotificationAsync(string message, NotificationType type, NotificationOptions? options = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Notification>> GetUnreadNotificationsAsync(string? userId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Notification>> GetAllNotificationsAsync(string? userId = null, CancellationToken cancellationToken = default);
    Task<IEnumerable<Notification>> GetNotificationsByCategoryAsync(string category, bool unreadOnly = false, string? userId = null, CancellationToken cancellationToken = default);
    Task MarkAsReadAsync(string notificationId, CancellationToken cancellationToken = default);
    Task MarkAllAsReadAsync(string? userId = null, CancellationToken cancellationToken = default);
    Task RemoveNotificationAsync(string notificationId, CancellationToken cancellationToken = default);
    Task ClearAllNotificationsAsync(string? userId = null, CancellationToken cancellationToken = default);
    Task ClearNotificationsByCategoryAsync(string category, string? userId = null, CancellationToken cancellationToken = default);
    Task ClearNotificationsByContextAsync(string context, string? userId = null, CancellationToken cancellationToken = default);
    Task<int> GetUnreadCountAsync(string? userId = null, CancellationToken cancellationToken = default);
}
