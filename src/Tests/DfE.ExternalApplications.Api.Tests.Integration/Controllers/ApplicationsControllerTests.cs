using System.Collections.ObjectModel;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Request;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.CoreLibs.Testing.Mocks.WebApplicationFactory;
using DfE.ExternalApplications.Tests.Common.Customizations;
using DfE.ExternalApplications.Tests.Common.Seeders;
using GovUK.Dfe.ExternalApplications.Api.Client.Contracts;
using System.Net.Http.Headers;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.CoreLibs.Http.Models;

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
        Assert.StartsWith("TRF-", result.ApplicationReference);
        Assert.NotNull(result.TemplateSchema);
        Assert.NotEqual(Guid.Empty, result.TemplateSchema.TemplateId);
        Assert.NotEqual(Guid.Empty, result.TemplateSchema.TemplateVersionId);
        Assert.NotEmpty(result.TemplateSchema.VersionNumber);
        Assert.NotEmpty(result.TemplateSchema.JsonSchema);
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

        var encodedBody = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(responseBody));
        var request = new AddApplicationResponseRequest
        {
            ResponseBody = encodedBody
        };

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
        var request = new AddApplicationResponseRequest
        {
            ResponseBody = responseBody
        };
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

        var request = new AddApplicationResponseRequest
        {
            ResponseBody = responseBody
        };
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

        var request = new AddApplicationResponseRequest
        {
            ResponseBody = string.Empty
        };
        // Act
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException<ExceptionResponse>>(
            () => applicationsClient.AddApplicationResponseAsync(new Guid(EaContextSeeder.ApplicationId), request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task AddApplicationResponseAsync_ShouldReturnBadRequest_WhenBodyIsNotBase64(
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

        var request = new AddApplicationResponseRequest
        {
            ResponseBody = "this is not base64"
        };
        
        // Act
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException<ExceptionResponse>>(
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
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException<ExceptionResponse>>(
            () => applicationsClient.CreateApplicationAsync(request));
        Assert.Equal(400, ex.StatusCode);
    }



    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetMyApplicationsAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.GetMyApplicationsAsync(includeSchema: null));
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
            () => applicationsClient.GetMyApplicationsAsync(includeSchema: null));
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
        Assert.NotNull(result.TemplateSchema);
        Assert.NotEqual(Guid.Empty, result.TemplateSchema.TemplateId);
        Assert.NotEqual(Guid.Empty, result.TemplateSchema.TemplateVersionId);
        Assert.NotEmpty(result.TemplateSchema.VersionNumber);
        Assert.NotEmpty(result.TemplateSchema.JsonSchema);
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

     [Theory]
     [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
     public async Task SubmitApplicationAsync_ShouldSubmitApplication_WhenValidRequest(
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

         var applicationId = Guid.Parse(EaContextSeeder.ApplicationId);

         // Act
         var result = await applicationsClient.SubmitApplicationAsync(applicationId);

         // Assert
         Assert.NotNull(result);
         Assert.Equal(applicationId, result.ApplicationId);
         Assert.Equal(ApplicationStatus.Submitted, result.Status);
         Assert.NotNull(result.DateSubmitted);
     }

     [Theory]
     [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
     public async Task SubmitApplicationAsync_ShouldReturnUnauthorized_WhenTokenMissing(
         CustomWebApplicationDbContextFactory<Program> factory,
         IApplicationsClient applicationsClient,
         HttpClient httpClient)
     {
         // Arrange
         var applicationId = Guid.Parse(EaContextSeeder.ApplicationId);

         // Act
         var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
             () => applicationsClient.SubmitApplicationAsync(applicationId));
         Assert.Equal(403, ex.StatusCode);
     }

     [Theory]
     [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
     public async Task SubmitApplicationAsync_ShouldReturnForbidden_WhenPermissionMissing(
         CustomWebApplicationDbContextFactory<Program> factory,
         IApplicationsClient applicationsClient,
         HttpClient httpClient)
     {
         // Arrange
         factory.TestClaims = new List<Claim>
         {
             new(ClaimTypes.Email, EaContextSeeder.BobEmail)
             // No Write permission for this application
         };

         httpClient.DefaultRequestHeaders.Authorization =
             new AuthenticationHeaderValue("Bearer", "user-token");

         var applicationId = Guid.Parse(EaContextSeeder.ApplicationId);

         // Act
         var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
             () => applicationsClient.SubmitApplicationAsync(applicationId));
         Assert.Equal(403, ex.StatusCode);
     }

     [Theory]
     [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
     public async Task SubmitApplicationAsync_ShouldReturnNotFound_WhenApplicationNotExists(
         CustomWebApplicationDbContextFactory<Program> factory,
         IApplicationsClient applicationsClient,
         HttpClient httpClient)
     {
         // Arrange
         var nonExistentApplicationId = Guid.NewGuid();
         
         factory.TestClaims = new List<Claim>
         {
             new(ClaimTypes.Email, EaContextSeeder.BobEmail),
             new("permission", $"Application:{nonExistentApplicationId}:Write") // Give permission for the specific non-existent app
         };

         httpClient.DefaultRequestHeaders.Authorization =
             new AuthenticationHeaderValue("Bearer", "user-token");

         // Act
         var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
             () => applicationsClient.SubmitApplicationAsync(nonExistentApplicationId));
         Assert.Equal(404, ex.StatusCode);
     }

     [Theory]
     [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
     public async Task SubmitApplicationAsync_ShouldReturnBadRequest_WhenApplicationAlreadySubmitted(
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

         var applicationId = Guid.Parse(EaContextSeeder.ApplicationId);

         // First submission
         await applicationsClient.SubmitApplicationAsync(applicationId);

         // Act - Try to submit again
         var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
             () => applicationsClient.SubmitApplicationAsync(applicationId));
         Assert.Equal(400, ex.StatusCode);
     }

     [Theory]
     [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
     public async Task SubmitApplicationAsync_ShouldReturnForbidden_WhenUserIsNotApplicationCreator(
         CustomWebApplicationDbContextFactory<Program> factory,
         IApplicationsClient applicationsClient,
         HttpClient httpClient)
     {
         // Arrange - Use Alice's email (Alice exists but didn't create the application - Bob did)
         factory.TestClaims = new List<Claim>
         {
             new(ClaimTypes.Email, "alice@example.com"), // Alice exists but didn't create the application
             new("permission", $"Application:{EaContextSeeder.ApplicationId}:Write")
         };

         httpClient.DefaultRequestHeaders.Authorization =
             new AuthenticationHeaderValue("Bearer", "user-token");

         var applicationId = Guid.Parse(EaContextSeeder.ApplicationId);

         // Act
         var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
             () => applicationsClient.SubmitApplicationAsync(applicationId));
         Assert.Equal(400, ex.StatusCode); // Should be 400 Bad Request with our error message
     }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetMyApplicationsAsync_ShouldReturnApplicationsWithoutSchema_WhenIncludeSchemaIsDefault(
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

        // Act - Testing default behavior (includeSchema defaults to false when null)
        var result = await applicationsClient.GetMyApplicationsAsync(includeSchema: null);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, app => 
        {
            Assert.Null(app.TemplateSchema);
        });
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetMyApplicationsAsync_ShouldReturnApplicationsWithSchema_WhenIncludeSchemaIsTrue(
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
        var result = await applicationsClient.GetMyApplicationsAsync(includeSchema: true);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, app => 
        {
            Assert.NotNull(app.TemplateSchema);
            Assert.NotEqual(Guid.Empty, app.TemplateSchema.TemplateId);
            Assert.NotEqual(Guid.Empty, app.TemplateSchema.TemplateVersionId);
            Assert.NotEmpty(app.TemplateSchema.VersionNumber);
            Assert.NotEmpty(app.TemplateSchema.JsonSchema);
        });
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetMyApplicationsAsync_ShouldReturnApplicationsWithoutSchema_WhenIncludeSchemaIsFalse(
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
        var result = await applicationsClient.GetMyApplicationsAsync(includeSchema: false);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, app => 
        {
            Assert.Null(app.TemplateSchema);
        });
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetApplicationsForUserAsync_ShouldReturnApplicationsWithSchema_WhenIncludeSchemaIsTrue(
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
        var result = await applicationsClient.GetApplicationsForUserAsync(EaContextSeeder.BobEmail, includeSchema: true);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, app => 
        {
            Assert.NotNull(app.TemplateSchema);
            Assert.NotEqual(Guid.Empty, app.TemplateSchema.TemplateId);
            Assert.NotEqual(Guid.Empty, app.TemplateSchema.TemplateVersionId);
            Assert.NotEmpty(app.TemplateSchema.VersionNumber);
            Assert.NotEmpty(app.TemplateSchema.JsonSchema);
        });
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetApplicationsForUserAsync_ShouldReturnApplicationsWithoutSchema_WhenIncludeSchemaIsFalse(
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
        var result = await applicationsClient.GetApplicationsForUserAsync(EaContextSeeder.BobEmail, includeSchema: false);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
        Assert.All(result, app => 
        {
            Assert.Null(app.TemplateSchema);
        });
     }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetContributorsAsync_ShouldReturnContributors_WhenValidRequest(
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
        var result = await applicationsClient.GetContributorsAsync(new Guid(EaContextSeeder.ApplicationId));

        // Assert
        Assert.NotNull(result);
        // Note: This will be empty initially since no contributors are seeded
        Assert.IsType<ObservableCollection<UserDto>>(result);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetContributorsAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient)
    {
        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.GetContributorsAsync(new Guid(EaContextSeeder.ApplicationId)));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetContributorsAsync_ShouldReturnForbidden_WhenPermissionMissing(
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

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.GetContributorsAsync(new Guid(EaContextSeeder.ApplicationId)));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task AddContributorAsync_ShouldAddContributor_WhenValidRequest(
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

        var request = new AddContributorRequest
        {
            Name = "John Doe",
            Email = "john.doe@example.com"
        };

        // Act
        var result = await applicationsClient.AddContributorAsync(new Guid(EaContextSeeder.ApplicationId), request);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("John Doe", result.Name);
        Assert.Equal("john.doe@example.com", result.Email);
        Assert.NotNull(result.Authorization);
        Assert.NotNull(result.Authorization.Permissions);
        Assert.NotNull(result.Authorization.Roles);
        Assert.NotEmpty(result.Authorization.Permissions);
        Assert.NotEmpty(result.Authorization.Roles);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task AddContributorAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient)
    {
        // Arrange
        var request = new AddContributorRequest
        {
            Name = "John Doe",
            Email = "john.doe@example.com"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.AddContributorAsync(new Guid(EaContextSeeder.ApplicationId), request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task AddContributorAsync_ShouldReturnForbidden_WhenPermissionMissing(
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

        var request = new AddContributorRequest
        {
            Name = "John Doe",
            Email = "john.doe@example.com"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.AddContributorAsync(new Guid(EaContextSeeder.ApplicationId), request));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task AddContributorAsync_ShouldReturnBadRequest_WhenInvalidData(
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

        var request = new AddContributorRequest
        {
            Name = "",
            Email = "invalid-email"
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException<ExceptionResponse>>(
            () => applicationsClient.AddContributorAsync(new Guid(EaContextSeeder.ApplicationId), request));
        Assert.Equal(400, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task RemoveContributorAsync_ShouldRemoveContributor_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"Application:{EaContextSeeder.ApplicationId}:Write"),
            new("permission", $"Application:{EaContextSeeder.ApplicationId}:Read")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // First, add a contributor
        var addRequest = new AddContributorRequest
        {
            Name = "John Doe",
            Email = "john.doe@example.com"
        };

        var addedContributor = await applicationsClient.AddContributorAsync(new Guid(EaContextSeeder.ApplicationId), addRequest);
        Assert.NotNull(addedContributor);

        // Act - Remove the contributor we just added
        await applicationsClient.RemoveContributorAsync(new Guid(EaContextSeeder.ApplicationId), addedContributor.UserId);

        // Assert
        // The endpoint should return 200 OK if successful
        // We can also verify the contributor was removed by trying to get contributors
        var contributors = await applicationsClient.GetContributorsAsync(new Guid(EaContextSeeder.ApplicationId));
        
        // Check if our specific contributor was removed
        var removedContributor = contributors.FirstOrDefault(c => c.UserId == addedContributor.UserId);
        Assert.Null(removedContributor);
        
        // Also verify that other contributors (if any) are still there
        var otherContributors = contributors.Where(c => c.UserId != addedContributor.UserId).ToList();
        Assert.Contains(otherContributors, c => c.Email == "alice@example.com");
        Assert.DoesNotContain(otherContributors, c => c.Email == "bob@example.com");
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task RemoveContributorAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient)
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.RemoveContributorAsync(new Guid(EaContextSeeder.ApplicationId), userId));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task RemoveContributorAsync_ShouldReturnForbidden_WhenPermissionMissing(
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

        var userId = Guid.NewGuid();

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ExternalApplicationsException>(
            () => applicationsClient.RemoveContributorAsync(new Guid(EaContextSeeder.ApplicationId), userId));
        Assert.Equal(403, ex.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task UploadFileAsync_ShouldUploadFile_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string description)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"ApplicationFiles:{EaContextSeeder.ApplicationId}:Write")
        };

        var fileName = "test-file.jpg";

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // Create a test file
        var fileContent = "Test file content";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var fileParameter = new FileParameter(stream, fileName, "text/plain");

        // Act
        var result = await applicationsClient.UploadFileAsync(
            new Guid(EaContextSeeder.ApplicationId),
            fileName,
            description,
            fileParameter);

        // Assert
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(new Guid(EaContextSeeder.ApplicationId), result.ApplicationId);
        Assert.Equal(fileName, result.Name);
        Assert.Equal(description, result.Description);
        Assert.Equal(fileName, result.OriginalFileName);
        Assert.NotNull(result.FileName);
        Assert.NotEmpty(result.FileName);
        Assert.NotEqual(default(DateTime), result.UploadedOn);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task UploadFileAsync_ShouldReturnUnauthorized_WhenTokenMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string fileName,
        string description)
    {
        // Arrange
        var fileContent = "Test file content";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var fileParameter = new FileParameter(stream, fileName, "text/plain");

        // Act
        var exception = await Assert.ThrowsAsync<ExternalApplicationsException>(() =>
            applicationsClient.UploadFileAsync(
                new Guid(EaContextSeeder.ApplicationId),
                fileName,
                description,
                fileParameter));

        // Assert
        Assert.Equal(403, exception.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task UploadFileAsync_ShouldReturnForbidden_WhenPermissionMissing(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string fileName,
        string description)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail)
            // No permission claim
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var fileContent = "Test file content";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var fileParameter = new FileParameter(stream, fileName, "text/plain");

        // Act
        var exception = await Assert.ThrowsAsync<ExternalApplicationsException>(() =>
            applicationsClient.UploadFileAsync(
                new Guid(EaContextSeeder.ApplicationId),
                fileName,
                description,
                fileParameter));

        // Assert
        Assert.Equal(403, exception.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task UploadFileAsync_ShouldReturnBadRequest_WhenNoFileProvided(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string fileName,
        string description)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"ApplicationFiles:{EaContextSeeder.ApplicationId}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // Act - No file parameter
        var exception = await Assert.ThrowsAsync<ExternalApplicationsException>(() =>
            applicationsClient.UploadFileAsync(
                new Guid(EaContextSeeder.ApplicationId),
                fileName,
                description,
                null));

        // Assert
        Assert.Equal(400, exception.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task UploadFileAsync_ShouldReturnBadRequest_WhenEmptyFile(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string fileName,
        string description)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"ApplicationFiles:{EaContextSeeder.ApplicationId}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // Create an empty file
        var stream = new MemoryStream();
        var fileParameter = new FileParameter(stream, fileName, "text/plain");

        // Act
        var exception = await Assert.ThrowsAsync<ExternalApplicationsException>(() =>
            applicationsClient.UploadFileAsync(
                new Guid(EaContextSeeder.ApplicationId),
                fileName,
                description,
                fileParameter));

        // Assert
        Assert.Equal(400, exception.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task UploadFileAsync_ShouldReturnNotFound_WhenApplicationNotExists(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string fileName,
        string description)
    {
        // Arrange
        var nonExistentApplicationId = Guid.NewGuid();
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"ApplicationFiles:{nonExistentApplicationId}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var fileContent = "Test file content";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var fileParameter = new FileParameter(stream, fileName, "text/plain");

        // Act
        var exception = await Assert.ThrowsAsync<ExternalApplicationsException>(() =>
            applicationsClient.UploadFileAsync(
                nonExistentApplicationId, // Use the same ID as in the permission claim
                fileName,
                description,
                fileParameter));

        // Assert
        Assert.Equal(404, exception.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task UploadFileAsync_ShouldReturnBadRequest_WhenFileAlreadyExists(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string description)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"ApplicationFiles:{EaContextSeeder.ApplicationId}:Write")
        };
        var fileName = "large-file.jpg";

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var fileContent = "Test file content";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var fileParameter = new FileParameter(stream, fileName, "text/plain");

        // Upload the same file twice
        await applicationsClient.UploadFileAsync(
            new Guid(EaContextSeeder.ApplicationId),
            fileName,
            description,
            fileParameter);

        // Create a new stream for the second upload
        var stream2 = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var fileParameter2 = new FileParameter(stream2, fileName, "text/plain");

        // Act - Try to upload the same file again
        var exception = await Assert.ThrowsAsync<ExternalApplicationsException>(() =>
            applicationsClient.UploadFileAsync(
                new Guid(EaContextSeeder.ApplicationId),
                fileName,
                description,
                fileParameter2));

        // Assert
        Assert.Equal(400, exception.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task UploadFileAsync_ShouldUploadFileWithNullDescription_WhenDescriptionIsNull(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"ApplicationFiles:{EaContextSeeder.ApplicationId}:Write")
        };
        var fileName = "large-file.jpg";

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var fileContent = "Test file content";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var fileParameter = new FileParameter(stream, fileName, "text/plain");

        // Act
        var result = await applicationsClient.UploadFileAsync(
            new Guid(EaContextSeeder.ApplicationId),
            fileName,
            null, // null description
            fileParameter);

        // Assert
        Assert.NotNull(result);
        Assert.Null(result.Description);
        Assert.Equal(fileName, result.Name);
        Assert.Equal(fileName, result.OriginalFileName);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task UploadFileAsync_ShouldReturnBadRequest_WhenNameIsNull(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string description)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"ApplicationFiles:{EaContextSeeder.ApplicationId}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var fileName = "test.txt";
        var fileContent = "Test file content";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var fileParameter = new FileParameter(stream, fileName, "text/plain");

        // Act
        var exception = await Assert.ThrowsAsync<ExternalApplicationsException<ExceptionResponse>>(() =>
            applicationsClient.UploadFileAsync(
                new Guid(EaContextSeeder.ApplicationId),
                null, // null name - should be rejected
                description,
                fileParameter));

        // Assert
        Assert.Equal(400, exception.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task UploadFileAsync_ShouldHandleLargeFile_WhenFileIsLarge(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string description)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"ApplicationFiles:{EaContextSeeder.ApplicationId}:Write")
        };

        var fileName = "large-file.jpg";
        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // Create a large file (1MB)
        var largeContent = new string('A', 1024 * 1024);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(largeContent));
        var fileParameter = new FileParameter(stream, fileName, "text/plain");

        // Act
        var result = await applicationsClient.UploadFileAsync(
            new Guid(EaContextSeeder.ApplicationId),
            fileName,
            description,
            fileParameter);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fileName, result.Name);
        Assert.Equal(description, result.Description);
        Assert.Equal(fileName, result.OriginalFileName);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task UploadFileAsync_ShouldHandleDifferentFileTypes_WhenFileTypeIsValid(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string description)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"ApplicationFiles:{EaContextSeeder.ApplicationId}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var testCases = new[]
        {
            new { FileName = "document.pdf", ContentType = "application/pdf", Content = "%PDF-1.4\nTest PDF content" },
            new { FileName = "image.jpg", ContentType = "image/jpeg", Content = "JPEG image content" },
            new { FileName = "spreadsheet.xlsx", ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", Content = "Excel content" },
            new { FileName = "document.docx", ContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document", Content = "Word document content" }
        };

        foreach (var testCase in testCases)
        {
            // Act
            var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(testCase.Content));
            var fileParameter = new FileParameter(stream, testCase.FileName, testCase.ContentType);

            var result = await applicationsClient.UploadFileAsync(
                new Guid(EaContextSeeder.ApplicationId),
                testCase.FileName,
                description,
                fileParameter);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(testCase.FileName, result.Name);
            Assert.Equal(testCase.FileName, result.OriginalFileName);
            Assert.Equal(description, result.Description);
        }
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task UploadFileAsync_ShouldReturnBadRequest_WhenFileSizeExceedsLimit(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string description)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"ApplicationFiles:{EaContextSeeder.ApplicationId}:Write")
        };
        var fileName = "large-file.jpg";

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // Create a file that exceeds the 25MB limit (26MB)
        var largeContent = new string('A', 26 * 1024 * 1024);
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(largeContent));
        var fileParameter = new FileParameter(stream, fileName, "text/plain");

        // Act
        var exception = await Assert.ThrowsAsync<ExternalApplicationsException>(() =>
            applicationsClient.UploadFileAsync(
                new Guid(EaContextSeeder.ApplicationId),
                fileName,
                description,
                fileParameter));

        // Assert
        Assert.Equal(400, exception.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task UploadFileAsync_ShouldReturnBadRequest_WhenFileExtensionNotAllowed(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient,
        string description)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"ApplicationFiles:{EaContextSeeder.ApplicationId}:Write")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        var fileName = "script.exe"; // Not allowed extension
        var fileContent = "Executable content";
        var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(fileContent));
        var fileParameter = new FileParameter(stream, fileName, "application/x-msdownload");

        // Act
        var exception = await Assert.ThrowsAsync<ExternalApplicationsException>(() =>
            applicationsClient.UploadFileAsync(
                new Guid(EaContextSeeder.ApplicationId),
                fileName,
                description,
                fileParameter));

        // Assert
        Assert.Equal(400, exception.StatusCode);
    }

    [Theory]
    [CustomAutoData(typeof(CustomWebApplicationDbContextFactoryCustomization))]
    public async Task GetFilesForApplicationAsync_ShouldReturnFiles_WhenValidRequest(
        CustomWebApplicationDbContextFactory<Program> factory,
        IApplicationsClient applicationsClient,
        HttpClient httpClient)
    {
        // Arrange
        factory.TestClaims = new List<Claim>
        {
            new(ClaimTypes.Email, EaContextSeeder.BobEmail),
            new("permission", $"ApplicationFiles:{EaContextSeeder.ApplicationId}:Read")
        };

        httpClient.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", "user-token");

        // Act
        var result = await applicationsClient.GetFilesForApplicationAsync(new Guid(EaContextSeeder.ApplicationId));

        // Assert
        Assert.NotNull(result);
        // Should return empty list since no files exist yet
        Assert.Empty(result);
    }
}  