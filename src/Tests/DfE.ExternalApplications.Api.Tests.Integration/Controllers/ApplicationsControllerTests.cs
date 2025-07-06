using System.Net;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Tests.Common.Seeders;
using System.Net.Http.Headers;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Request;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Application.Applications.Commands;
using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;
using GovUK.Dfe.ExternalApplications.Api.Client;

namespace DfE.ExternalApplications.Api.Tests.Integration.Controllers;

public class ApplicationsControllerTests
{
    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateApplicationAsync_ShouldCreateApplication_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string initialResponseBody)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Template:{EaContextSeeder.TemplateId}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var request = new CreateApplicationRequest
        {
            TemplateId = Guid.Parse(EaContextSeeder.TemplateId),
            InitialResponseBody = initialResponseBody
        };

        // Act
        var result = await applicationsClient.CreateApplicationAsync(request);

        // Assert
        Assert.NotNull(result);
        Assert.StartsWith("APP-", result.ApplicationReference);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task AddApplicationResponseAsync_ShouldAddResponse_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string responseBody)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Application:{EaContextSeeder.ApplicationId}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var request = new AddApplicationResponseRequest(responseBody);

        // Act
        var response = await applicationsClient.AddApplicationResponseAsync(new Guid(EaContextSeeder.ApplicationId), request);

        // Assert
        Assert.NotNull(response);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task AddApplicationResponseAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,

        HttpClient httpClient,
        string responseBody)
    {
        // Arrange
        var request = new AddApplicationResponseRequest(responseBody);
        var requestJson = System.Text.Json.JsonSerializer.Serialize(request);
        var content = new StringContent(requestJson, System.Text.Encoding.UTF8, "application/json");

        // Act
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.AddApplicationResponseAsync(new Guid(EaContextSeeder.ApplicationId), request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task AddApplicationResponseAsync_ShouldReturnForbidden_WhenPermissionMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,

        HttpClient httpClient,
        string responseBody)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail)
            // No permission claim
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        var request = new AddApplicationResponseRequest(responseBody);

        // Act
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.AddApplicationResponseAsync(new Guid(EaContextSeeder.ApplicationId), request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task AddApplicationResponseAsync_ShouldReturnBadRequest_WhenInvalidData(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,

        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Application:{EaContextSeeder.ApplicationId}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var request = new AddApplicationResponseRequest(string.Empty); // Invalid empty response body

        // Act
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.AddApplicationResponseAsync(new Guid(EaContextSeeder.ApplicationId), request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateApplicationAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        CreateApplicationRequest request)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.CreateApplicationAsync(request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateApplicationAsync_ShouldReturnForbidden_WhenPermissionMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        CreateApplicationRequest request)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail)
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.CreateApplicationAsync(request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task CreateApplicationAsync_ShouldReturnBadRequest_WhenInvalidData(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Template:{EaContextSeeder.TemplateId}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var request = new CreateApplicationRequest
        {
            TemplateId = Guid.Parse(EaContextSeeder.TemplateId),
            InitialResponseBody = string.Empty
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.CreateApplicationAsync(request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetMyApplicationsAsync_ShouldReturnApplications_WhenUserHasAccess(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", "Application:Read")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        // Act
        var result = await applicationsClient.GetMyApplicationsAsync();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetMyApplicationsAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.GetMyApplicationsAsync());
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetMyApplicationsAsync_ShouldReturnForbidden_WhenPermissionMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail)
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "test-token");

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.GetMyApplicationsAsync());
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetApplicationByReferenceAsync_ShouldReturnApplicationDetails_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Application:{EaContextSeeder.ApplicationId}:Read")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // Act
        var result = await applicationsClient.GetApplicationByReferenceAsync(EaContextSeeder.ApplicationReference);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(EaContextSeeder.ApplicationReference, result.ApplicationReference);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetApplicationByReferenceAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient)
    {
        // Act
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.GetApplicationByReferenceAsync(EaContextSeeder.ApplicationReference));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetApplicationByReferenceAsync_ShouldReturnForbidden_WhenPermissionMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail)
            // No permission claim
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // Act
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.GetApplicationByReferenceAsync(EaContextSeeder.ApplicationReference));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetApplicationByReferenceAsync_ShouldReturnNotFound_WhenApplicationNotExists(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Application:{EaContextSeeder.ApplicationId}:Read")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // Act
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.GetApplicationByReferenceAsync("InvalidAppRef"));
        Assert.Equal(404, ex.StatusCode);
    }
} 