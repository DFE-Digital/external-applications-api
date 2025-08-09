using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Tests.Common.Seeders;
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

namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers;

public class NotificationsControllerTests
{
    [Theory]
    [CustomAutoData(typeof(NotificationTestCustomization))]
    public async Task CreateNotificationAsync_ShouldCreateNotification_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
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

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await httpClient.PostAsync("/v1/notifications", content);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<NotificationDto>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

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
    [CustomAutoData(typeof(NotificationTestCustomization))]
    public async Task CreateNotificationAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        HttpClient httpClient)
    {
        // Arrange
        var request = new AddNotificationRequest
        {
            Message = "Test notification message",
            Type = NotificationType.Info
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await httpClient.PostAsync("/v1/notifications", content);

        // Assert
        Assert.Equal(403, (int)response.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(NotificationTestCustomization))]
    public async Task CreateNotificationAsync_ShouldReturnBadRequest_WhenInvalidData(
        CustomWebApplicationDbContextFactory<Program> factory,
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

        var request = new AddNotificationRequest
        {
            Message = "", // Invalid - empty message
            Type = NotificationType.Info // Valid type, but message is invalid
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        // Act
        var response = await httpClient.PostAsync("/v1/notifications", content);

        // Assert
        Assert.Equal(400, (int)response.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(NotificationTestCustomization))]
    public async Task GetUnreadNotificationsAsync_ShouldReturnNotifications_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
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

        var createJson = JsonSerializer.Serialize(createRequest);
        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
        await httpClient.PostAsync("/v1/notifications", createContent);

        // Act
        var response = await httpClient.GetAsync("/v1/notifications/unread");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<IEnumerable<NotificationDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, n => Assert.False(n.IsRead));
    }

    [Theory]
    [CustomAutoData(typeof(NotificationTestCustomization))]
    public async Task GetAllNotificationsAsync_ShouldReturnAllNotifications_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
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
        var response = await httpClient.GetAsync("/v1/notifications");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<IEnumerable<NotificationDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
    }

    [Theory]
    [CustomAutoData(typeof(NotificationTestCustomization))]
    public async Task GetNotificationsByCategoryAsync_ShouldReturnFilteredNotifications_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
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

        // Create a notification with specific category
        var createRequest = new AddNotificationRequest
        {
            Message = "Test notification for category filter",
            Type = NotificationType.Info,
            Category = "TestCategory"
        };

        var createJson = JsonSerializer.Serialize(createRequest);
        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
        await httpClient.PostAsync("/v1/notifications", createContent);

        // Act
        var response = await httpClient.GetAsync("/v1/notifications/category/TestCategory");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<IEnumerable<NotificationDto>>(responseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        Assert.NotNull(result);
        Assert.All(result, n => Assert.Equal("TestCategory", n.Category));
    }

    [Theory]
    [CustomAutoData(typeof(NotificationTestCustomization))]
    public async Task GetUnreadNotificationCountAsync_ShouldReturnCount_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
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
        var response = await httpClient.GetAsync("/v1/notifications/unread/count");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        var responseContent = await response.Content.ReadAsStringAsync();
        var result = JsonSerializer.Deserialize<int>(responseContent);

        Assert.True(result >= 0);
    }

    [Theory]
    [CustomAutoData(typeof(NotificationTestCustomization))]
    public async Task MarkNotificationAsReadAsync_ShouldMarkAsRead_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
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
            Message = "Test notification for marking as read",
            Type = NotificationType.Info
        };

        var createJson = JsonSerializer.Serialize(createRequest);
        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
        var createResponse = await httpClient.PostAsync("/v1/notifications", createContent);
        var createResponseContent = await createResponse.Content.ReadAsStringAsync();
        var createdNotification = JsonSerializer.Deserialize<NotificationDto>(createResponseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act
        var response = await httpClient.PutAsync($"/v1/notifications/{createdNotification!.Id}/read", null);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(NotificationTestCustomization))]
    public async Task MarkAllNotificationsAsReadAsync_ShouldMarkAllAsRead_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
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

        // Act
        var response = await httpClient.PutAsync("/v1/notifications/read-all", null);

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(NotificationTestCustomization))]
    public async Task RemoveNotificationAsync_ShouldRemoveNotification_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
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
            Message = "Test notification for removal",
            Type = NotificationType.Info
        };

        var createJson = JsonSerializer.Serialize(createRequest);
        var createContent = new StringContent(createJson, Encoding.UTF8, "application/json");
        var createResponse = await httpClient.PostAsync("/v1/notifications", createContent);
        var createResponseContent = await createResponse.Content.ReadAsStringAsync();
        var createdNotification = JsonSerializer.Deserialize<NotificationDto>(createResponseContent, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        // Act
        var response = await httpClient.DeleteAsync($"/v1/notifications/{createdNotification!.Id}");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(NotificationTestCustomization))]
    public async Task ClearAllNotificationsAsync_ShouldClearAll_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
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

        // Act
        var response = await httpClient.DeleteAsync("/v1/notifications/clear-all");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(NotificationTestCustomization))]
    public async Task ClearNotificationsByCategoryAsync_ShouldClearByCategory_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
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

        // Act
        var response = await httpClient.DeleteAsync("/v1/notifications/category/TestCategory");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(NotificationTestCustomization))]
    public async Task ClearNotificationsByContextAsync_ShouldClearByContext_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
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

        // Act
        var response = await httpClient.DeleteAsync("/v1/notifications/context/TestContext");

        // Assert
        Assert.True(response.IsSuccessStatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(NotificationTestCustomization))]
    public async Task NotificationEndpoints_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        HttpClient httpClient)
    {
        // Test all GET endpoints
        var getEndpoints = new[]
        {
            "/v1/notifications",
            "/v1/notifications/unread",
            "/v1/notifications/unread/count",
            "/v1/notifications/category/test"
        };

        foreach (var endpoint in getEndpoints)
        {
            var response = await httpClient.GetAsync(endpoint);
            Assert.Equal(403, (int)response.StatusCode);
        }

        // Test PUT endpoints
        var putEndpoints = new[]
        {
            "/v1/notifications/test-id/read",
            "/v1/notifications/read-all"
        };

        foreach (var endpoint in putEndpoints)
        {
            var response = await httpClient.PutAsync(endpoint, null);
            Assert.Equal(403, (int)response.StatusCode);
        }

        // Test DELETE endpoints
        var deleteEndpoints = new[]
        {
            "/v1/notifications/test-id",
            "/v1/notifications/clear-all",
            "/v1/notifications/category/test",
            "/v1/notifications/context/test"
        };

        foreach (var endpoint in deleteEndpoints)
        {
            var response = await httpClient.DeleteAsync(endpoint);
            Assert.Equal(403, (int)response.StatusCode);
        }
    }
}
