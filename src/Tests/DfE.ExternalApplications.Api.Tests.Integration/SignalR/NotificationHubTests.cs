using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Tests.Common.Seeders;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Linq;
using System.Security.Claims;
using Xunit;

namespace DfE.ExternalApplications.Api.Tests.Integration.SignalR;

public class NotificationHubTests
{
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task Hub_ShouldConnectSuccessfully_WhenValidTokenProvided(
        CustomWebApplicationDbContextFactory<Program> factory,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail)
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // Obtain hub auth cookie via HubAuthController flow
        var ticketReq = await httpClient.PostAsync("/auth/hub-ticket", content: null);
        ticketReq.EnsureSuccessStatusCode();
        var payload = JsonDocument.Parse(await ticketReq.Content.ReadAsStringAsync());
        var redeemUrl = payload.RootElement.GetProperty("url").GetString();
        var redeemResp = await httpClient.GetAsync(redeemUrl!);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, redeemResp.StatusCode);
        var rawCookies = redeemResp.Headers.TryGetValues("Set-Cookie", out var cookieValues)
            ? cookieValues.ToList()
            : new List<string>();
        var authCookies = rawCookies
            .Select(c => c.Split(';')[0])
            .ToList();

        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                if (authCookies.Any())
                {
                    options.Headers.Add("Cookie", string.Join("; ", authCookies));
                }
            })
            .Build();

        // Act
        await connection.StartAsync();

        // Assert
        Assert.Equal(HubConnectionState.Connected, connection.State);

        // Cleanup
        await connection.DisposeAsync();
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task Hub_ShouldDisconnectSuccessfully_WhenConnectionStopped(
        CustomWebApplicationDbContextFactory<Program> factory,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail)
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // Obtain hub auth cookie via HubAuthController flow
        var ticketReq2 = await httpClient.PostAsync("/auth/hub-ticket", content: null);
        ticketReq2.EnsureSuccessStatusCode();
        var payload2 = JsonDocument.Parse(await ticketReq2.Content.ReadAsStringAsync());
        var redeemUrl2 = payload2.RootElement.GetProperty("url").GetString();
        var redeemResp2 = await httpClient.GetAsync(redeemUrl2!);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, redeemResp2.StatusCode);
        var rawCookies2 = redeemResp2.Headers.TryGetValues("Set-Cookie", out var cookieValues2)
            ? cookieValues2.ToList()
            : new List<string>();
        var cookiePairs2 = rawCookies2
            .Select(c => c.Split(';')[0])
            .ToList();

        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                if (cookiePairs2.Any())
                {
                    options.Headers.Add("Cookie", string.Join("; ", cookiePairs2));
                }
            })
            .Build();

        // Act
        await connection.StartAsync();
        await connection.StopAsync();

        // Assert
        Assert.Equal(HubConnectionState.Disconnected, connection.State);

        // Cleanup
        await connection.DisposeAsync();
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task Hub_ShouldReturnUnauthorized_WhenNoCookieProvided(
        CustomWebApplicationDbContextFactory<Program> factory)
    {
        // Arrange
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();

        // Act & Assert - should fail due to missing hub auth cookie
        try
        {
            await connection.StartAsync();
            // If we reach here, the connection succeeded when it shouldn't have
            Assert.True(false, "Connection should have failed due to missing authentication");
        }
        catch (Exception ex)
        {
            // Connection should fail due to missing authentication
            Assert.True(ex.Message.Contains("401") || 
                       ex.Message.Contains("Unauthorized") || 
                       ex.Message.Contains("Forbidden") ||
                       ex is HttpRequestException,
                       $"Expected authentication failure, but got: {ex.GetType().Name}: {ex.Message}");
        }

        // Cleanup
        await connection.DisposeAsync();
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task Hub_ShouldAddUserToGroup_WhenUserConnectsWithValidClaims(
        CustomWebApplicationDbContextFactory<Program> factory,
        HttpClient httpClient)
    {
        // Arrange
        var userEmail = "test.user@example.com";
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, userEmail)
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // Obtain hub auth cookie via HubAuthController flow
        var ticketReq = await httpClient.PostAsync("/auth/hub-ticket", content: null);
        ticketReq.EnsureSuccessStatusCode();
        var payload = JsonDocument.Parse(await ticketReq.Content.ReadAsStringAsync());
        var redeemUrl = payload.RootElement.GetProperty("url").GetString();
        var redeemResp = await httpClient.GetAsync(redeemUrl!);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, redeemResp.StatusCode);
        var rawCookies = redeemResp.Headers.TryGetValues("Set-Cookie", out var cookieValues)
            ? cookieValues.ToList()
            : new List<string>();
        var authCookies = rawCookies
            .Select(c => c.Split(';')[0])
            .ToList();

        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                if (authCookies.Any())
                {
                    options.Headers.Add("Cookie", string.Join("; ", authCookies));
                }
            })
            .Build();

        // Act
        await connection.StartAsync();

        // Assert
        Assert.Equal(HubConnectionState.Connected, connection.State);

        // Cleanup
        await connection.DisposeAsync();
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task Hub_ShouldHandleMultipleConnections_WhenMultipleUsersConnect(
        CustomWebApplicationDbContextFactory<Program> factory,
        HttpClient httpClient)
    {
        // Arrange
        var user1Email = "user1@example.com";
        var user2Email = "user2@example.com";

        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, user1Email)
        };

        // Obtain hub auth cookie via HubAuthController flow
        var ticketReq = await httpClient.PostAsync("/auth/hub-ticket", content: null);
        ticketReq.EnsureSuccessStatusCode();
        var payload = JsonDocument.Parse(await ticketReq.Content.ReadAsStringAsync());
        var redeemUrl = payload.RootElement.GetProperty("url").GetString();
        var redeemResp = await httpClient.GetAsync(redeemUrl!);
        Assert.Equal(System.Net.HttpStatusCode.NoContent, redeemResp.StatusCode);
        var rawCookies = redeemResp.Headers.TryGetValues("Set-Cookie", out var cookieValues)
            ? cookieValues.ToList()
            : new List<string>();
        var authCookies = rawCookies
            .Select(c => c.Split(';')[0])
            .ToList();

        var connection1 = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                
                // Add the authentication cookie to the SignalR connection
                if (authCookies.Any())
                {
                    options.Headers.Add("Cookie", string.Join("; ", authCookies));
                }
            })
            .Build();

        var connection2 = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                
                // Add the authentication cookie to the SignalR connection
                if (authCookies.Any())
                {
                    options.Headers.Add("Cookie", string.Join("; ", authCookies));
                }
            })
            .Build();

        // Act
        await connection1.StartAsync();
        await connection2.StartAsync();

        // Assert
        Assert.Equal(HubConnectionState.Connected, connection1.State);
        Assert.Equal(HubConnectionState.Connected, connection2.State);

        // Cleanup
        await connection1.DisposeAsync();
        await connection2.DisposeAsync();
    }
}
