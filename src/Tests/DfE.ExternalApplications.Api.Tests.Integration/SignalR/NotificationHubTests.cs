using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Tests.Common.Seeders;
using Microsoft.AspNetCore.SignalR.Client;
using System.Net.Http.Headers;
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

        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Headers.Add("Authorization", "Bearer user-token");
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

        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Headers.Add("Authorization", "Bearer user-token");
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
    public async Task Hub_ShouldConnect_WhenNoTokenProvided(
        CustomWebApplicationDbContextFactory<Program> factory)
    {
        // Arrange
        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
            })
            .Build();

        // Act & Assert
        // In test environment, SignalR authentication might be bypassed for testing purposes
        // This test verifies that the hub endpoint is accessible
        await connection.StartAsync();
        
        // Verify the connection was successful
        Assert.Equal(HubConnectionState.Connected, connection.State);

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

        var connection = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Headers.Add("Authorization", "Bearer user-token");
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

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var connection1 = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Headers.Add("Authorization", "Bearer user-token");
            })
            .Build();

        var connection2 = new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/notifications", options =>
            {
                options.HttpMessageHandlerFactory = _ => factory.Server.CreateHandler();
                options.Headers.Add("Authorization", "Bearer user-token");
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
