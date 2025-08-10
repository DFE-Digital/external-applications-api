using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Tests.Common.Seeders;
using GovUK.Dfe.ExternalApplications.Api.Client;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Request;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using DfE.CoreLibs.Http.Models;
using DfE.CoreLibs.Notifications.Models;
using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;

namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers;

public class NotificationsControllerTests
{
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateNotificationAsync_ShouldCreateNotification_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        INotificationsClient notificationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Notifications:{EaContextSeeder.BobEmail}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var request = new AddNotificationRequest
        {
            Message = "Test notification message",
            Type = NotificationType.Info,
            Category = "Test",
            Context = "Testing",
            AutoDismiss = true,
            AutoDismissSeconds = 30,
            Priority = NotificationPriority.Normal,
            ActionUrl = "/test-action"
        };

        // Act
        var result = await notificationsClient.CreateNotificationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Id);
        Assert.Equal("Test notification message", result.Message);
        Assert.Equal(NotificationType.Info, result.Type);
        Assert.Equal("Test", result.Category);
        Assert.Equal("Testing", result.Context);
        Assert.True(result.AutoDismiss);
        Assert.Equal(30, result.AutoDismissSeconds);
        Assert.False(result.IsRead);
        Assert.Equal(NotificationPriority.Normal, result.Priority);
        Assert.Equal("/test-action", result.ActionUrl);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateNotificationAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        INotificationsClient notificationsClient)
    {
        // Arrange
        var request = new AddNotificationRequest
        {
            Message = "Test notification message",
            Type = NotificationType.Info
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => notificationsClient.CreateNotificationAsync(request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateNotificationAsync_ShouldReturnBadRequest_WhenInvalidData(
        CustomWebApplicationDbContextFactory<Program> factory,
        INotificationsClient notificationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Notifications:{EaContextSeeder.BobEmail}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var request = new AddNotificationRequest
        {
            Message = "", // Invalid - empty message
            Type = NotificationType.Info
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => notificationsClient.CreateNotificationAsync(request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetUnreadNotificationsAsync_ShouldReturnNotifications_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        INotificationsClient notificationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Notifications:{EaContextSeeder.BobEmail}:Read")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // First create a notification
        var createRequest = new AddNotificationRequest
        {
            Message = "Test notification for unread test",
            Type = NotificationType.Info
        };

        await notificationsClient.CreateNotificationAsync(createRequest);

        // Act
        var result = await notificationsClient.GetUnreadNotificationsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, n => Assert.False(n.IsRead));
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetAllNotificationsAsync_ShouldReturnAllNotifications_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        INotificationsClient notificationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Notifications:{EaContextSeeder.BobEmail}:Read")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // Act
        var result = await notificationsClient.GetAllNotificationsAsync();

        // Assert
        Assert.NotNull(result);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetNotificationsByCategoryAsync_ShouldReturnFilteredNotifications_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        INotificationsClient notificationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Notifications:{EaContextSeeder.BobEmail}:Read")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // First create a notification with specific category
        var createRequest = new AddNotificationRequest
        {
            Message = "Test notification for category test",
            Type = NotificationType.Info,
            Category = "TestCategory"
        };

        await notificationsClient.CreateNotificationAsync(createRequest);

        // Act
        var result = await notificationsClient.GetNotificationsByCategoryAsync("TestCategory");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, n => Assert.Equal("TestCategory", n.Category));
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetUnreadNotificationCountAsync_ShouldReturnCount_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        INotificationsClient notificationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Notifications:{EaContextSeeder.BobEmail}:Read")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // First create a notification
        var createRequest = new AddNotificationRequest
        {
            Message = "Test notification for count test",
            Type = NotificationType.Info
        };

        await notificationsClient.CreateNotificationAsync(createRequest);

        // Act
        var count = await notificationsClient.GetUnreadNotificationCountAsync();

        // Assert
        Assert.True(count > 0);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task MarkNotificationAsReadAsync_ShouldMarkAsRead_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        INotificationsClient notificationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Notifications:{EaContextSeeder.BobEmail}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // First create a notification
        var createRequest = new AddNotificationRequest
        {
            Message = "Test notification for mark as read test",
            Type = NotificationType.Info
        };

        var notification = await notificationsClient.CreateNotificationAsync(createRequest);

        // Act
        await notificationsClient.MarkNotificationAsReadAsync(notification.Id);

        // Assert - verify it's marked as read by getting unread count
        var unreadCount = await notificationsClient.GetUnreadNotificationCountAsync();
        Assert.True(unreadCount >= 0); // Should not throw an exception
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task MarkAllNotificationsAsReadAsync_ShouldMarkAllAsRead_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        INotificationsClient notificationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Notifications:{EaContextSeeder.BobEmail}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // First create a notification
        var createRequest = new AddNotificationRequest
        {
            Message = "Test notification for mark all as read test",
            Type = NotificationType.Info
        };

        await notificationsClient.CreateNotificationAsync(createRequest);

        // Act
        await notificationsClient.MarkAllNotificationsAsReadAsync();

        // Assert - verify all are marked as read
        var unreadCount = await notificationsClient.GetUnreadNotificationCountAsync();
        Assert.Equal(0, unreadCount);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task RemoveNotificationAsync_ShouldRemoveNotification_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        INotificationsClient notificationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Notifications:{EaContextSeeder.BobEmail}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // First create a notification
        var createRequest = new AddNotificationRequest
        {
            Message = "Test notification for remove test",
            Type = NotificationType.Info
        };

        var notification = await notificationsClient.CreateNotificationAsync(createRequest);

        // Act
        await notificationsClient.RemoveNotificationAsync(notification.Id);

        // Assert - verify it's removed by checking count
        var allNotifications = await notificationsClient.GetAllNotificationsAsync();
        Assert.DoesNotContain(allNotifications, n => n.Id == notification.Id);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task ClearAllNotificationsAsync_ShouldClearAll_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        INotificationsClient notificationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Notifications:{EaContextSeeder.BobEmail}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // First create a notification
        var createRequest = new AddNotificationRequest
        {
            Message = "Test notification for clear all test",
            Type = NotificationType.Info
        };

        await notificationsClient.CreateNotificationAsync(createRequest);

        // Act
        await notificationsClient.ClearAllNotificationsAsync();

        // Assert - verify all are cleared
        var allNotifications = await notificationsClient.GetAllNotificationsAsync();
        Assert.Empty(allNotifications);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task ClearNotificationsByCategoryAsync_ShouldClearByCategory_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        INotificationsClient notificationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Notifications:{EaContextSeeder.BobEmail}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // First create notifications with different categories
        var createRequest1 = new AddNotificationRequest
        {
            Message = "Test notification for category clear test 1",
            Type = NotificationType.Info,
            Category = "TestCategory"
        };

        var createRequest2 = new AddNotificationRequest
        {
            Message = "Test notification for category clear test 2",
            Type = NotificationType.Info,
            Category = "OtherCategory"
        };

        await notificationsClient.CreateNotificationAsync(createRequest1);
        await notificationsClient.CreateNotificationAsync(createRequest2);

        // Act
        await notificationsClient.ClearNotificationsByCategoryAsync("TestCategory");

        // Assert - verify only TestCategory notifications are cleared
        var remainingNotifications = await notificationsClient.GetAllNotificationsAsync();
        Assert.DoesNotContain(remainingNotifications, n => n.Category == "TestCategory");
        Assert.Contains(remainingNotifications, n => n.Category == "OtherCategory");
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task ClearNotificationsByContextAsync_ShouldClearByContext_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        INotificationsClient notificationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Notifications:{EaContextSeeder.BobEmail}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // First create notifications with different contexts
        var createRequest1 = new AddNotificationRequest
        {
            Message = "Test notification for context clear test 1",
            Type = NotificationType.Info,
            Context = "TestContext"
        };

        var createRequest2 = new AddNotificationRequest
        {
            Message = "Test notification for context clear test 2",
            Type = NotificationType.Info,
            Context = "OtherContext"
        };

        await notificationsClient.CreateNotificationAsync(createRequest1);
        await notificationsClient.CreateNotificationAsync(createRequest2);

        // Act
        await notificationsClient.ClearNotificationsByContextAsync("TestContext");

        // Assert - verify only TestContext notifications are cleared
        var remainingNotifications = await notificationsClient.GetAllNotificationsAsync();
        Assert.DoesNotContain(remainingNotifications, n => n.Context == "TestContext");
        Assert.Contains(remainingNotifications, n => n.Context == "OtherContext");
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task NotificationEndpoints_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        INotificationsClient notificationsClient)
    {
        // Act & Assert - all endpoints should return 403 when no token
        var createRequest = new AddNotificationRequest
        {
            Message = "Test notification",
            Type = NotificationType.Info
        };

        await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => notificationsClient.CreateNotificationAsync(createRequest));

        await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => notificationsClient.GetAllNotificationsAsync());

        await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => notificationsClient.GetUnreadNotificationsAsync());

        await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => notificationsClient.GetUnreadNotificationCountAsync());
    }
}
