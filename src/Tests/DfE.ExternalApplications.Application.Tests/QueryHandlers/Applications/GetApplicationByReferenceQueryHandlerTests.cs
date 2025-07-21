using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using Microsoft.AspNetCore.Http;
using MockQueryable.NSubstitute;
using NSubstitute;
using System.Security.Claims;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Applications;

public class GetApplicationByReferenceQueryHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(ApplicationResponseCustomization), typeof(UserCustomization))]
    public async Task Handle_ShouldReturnApplicationDetails_WhenValidRequestWithAppId(
        GetApplicationByReferenceQuery query,
        User user,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IPermissionCheckerService permissionCheckerService)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var externalId = "external-id";
        var claims = new List<Claim>
        {
            new("appid", externalId),
            new(ClaimTypes.Email, "test@example.com")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        // Create application with response
        var applicationId = new ApplicationId(Guid.NewGuid());
        var templateVersionId = new TemplateVersionId(Guid.NewGuid());
        var responseId = new ResponseId(Guid.NewGuid());

        var template = new Template(
            new TemplateId(Guid.NewGuid()),
            "Test Template",
            DateTime.UtcNow,
            user.Id!);

        var templateVersion = new TemplateVersion(
            templateVersionId,
            template.Id!,
            "1.0",
            "{}",
            DateTime.UtcNow,
            user.Id!);
        templateVersion.GetType().GetProperty("Template")?.SetValue(templateVersion, template);

        var application = new Domain.Entities.Application(
            applicationId,
            query.ApplicationReference,
            templateVersionId,
            DateTime.UtcNow,
            user.Id!);

        application.GetType().GetProperty("TemplateVersion")?.SetValue(application, templateVersion);

        var response = new ApplicationResponse(
            responseId,
            applicationId,
            "Test response body",
            DateTime.UtcNow,
            user.Id!);

        application.GetType().GetProperty("CreatedByUser")?.SetValue(application, user);

        application.AddResponse(response);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        permissionCheckerService.HasPermission(
            ResourceType.Application,
            applicationId.Value.ToString(),
            AccessType.Read)
            .Returns(true);

        var handler = new GetApplicationByReferenceQueryHandler(
            applicationRepo,
            httpContextAccessor,
            permissionCheckerService);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(query.ApplicationReference, result.Value.ApplicationReference);
        Assert.Equal(applicationId.Value, result.Value.ApplicationId);
        Assert.Equal(templateVersionId.Value, result.Value.TemplateVersionId);
        Assert.Equal("Test Template", result.Value.TemplateName);
        Assert.NotNull(result.Value.LatestResponse);
        Assert.Equal(responseId.Value, result.Value.LatestResponse.ResponseId);
        Assert.Equal("Test response body", result.Value.LatestResponse.ResponseBody);
        Assert.NotNull(result.Value.TemplateSchema);
        Assert.Equal(template.Id!.Value, result.Value.TemplateSchema.TemplateId);
        Assert.Equal(templateVersionId.Value, result.Value.TemplateSchema.TemplateVersionId);
        Assert.Equal("1.0", result.Value.TemplateSchema.VersionNumber);
        Assert.Equal("{}", result.Value.TemplateSchema.JsonSchema);

        Assert.NotNull(result.Value.CreatedBy);
        Assert.Equal(user.Id.Value, result.Value.CreatedBy.UserId);
        Assert.Equal(user.Name, result.Value.CreatedBy.Name);
        Assert.Equal(user.Email, result.Value.CreatedBy.Email);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_ShouldReturnApplicationDetails_WhenValidRequestWithEmail(
        GetApplicationByReferenceQuery query,
        User user,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IPermissionCheckerService permissionCheckerService)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var applicationId = new ApplicationId(Guid.NewGuid());
        var templateVersionId = new TemplateVersionId(Guid.NewGuid());
        
        var template = new Template(
            new TemplateId(Guid.NewGuid()),
            "Test Template",
            DateTime.UtcNow,
            user.Id!);

        var templateVersion = new TemplateVersion(
            templateVersionId,
            template.Id!,
            "1.0",
            "{}",
            DateTime.UtcNow,
            user.Id!);
        templateVersion.GetType().GetProperty("Template")?.SetValue(templateVersion, template);

        var application = new Domain.Entities.Application(
            applicationId,
            query.ApplicationReference,
            templateVersionId,
            DateTime.UtcNow,
            user.Id!);

        application.GetType().GetProperty("TemplateVersion")?.SetValue(application, templateVersion);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        permissionCheckerService.HasPermission(
            ResourceType.Application,
            applicationId.Value.ToString(),
            AccessType.Read)
            .Returns(true);

        var handler = new GetApplicationByReferenceQueryHandler(
            applicationRepo,
            httpContextAccessor,
            permissionCheckerService);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(query.ApplicationReference, result.Value.ApplicationReference);
        Assert.Null(result.Value.LatestResponse); // No responses added
        Assert.NotNull(result.Value.TemplateSchema);
        Assert.Equal(template.Id!.Value, result.Value.TemplateSchema.TemplateId);
        Assert.Equal(templateVersionId.Value, result.Value.TemplateSchema.TemplateVersionId);
        Assert.Equal("1.0", result.Value.TemplateSchema.VersionNumber);
        Assert.Equal("{}", result.Value.TemplateSchema.JsonSchema);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserNotAuthenticated(
        GetApplicationByReferenceQuery query,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IPermissionCheckerService permissionCheckerService)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new GetApplicationByReferenceQueryHandler(
            applicationRepo,
            httpContextAccessor,
            permissionCheckerService);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnApplicationNotFound_WhenApplicationDoesNotExist(
        GetApplicationByReferenceQuery query,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IPermissionCheckerService permissionCheckerService)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var applications = Array.Empty<Domain.Entities.Application>().AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        var handler = new GetApplicationByReferenceQueryHandler(
            applicationRepo,
            httpContextAccessor,
            permissionCheckerService);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Application not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_ShouldReturnForbidden_WhenUserHasNoPermission(
        GetApplicationByReferenceQuery query,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        User user,
        IPermissionCheckerService permissionCheckerService)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var applicationId = new ApplicationId(Guid.NewGuid());
        var application = new Domain.Entities.Application(
            applicationId,
            query.ApplicationReference,
            new TemplateVersionId(Guid.NewGuid()),
            DateTime.UtcNow,
            user.Id!);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        // User has no permission
        permissionCheckerService.HasPermission(
            ResourceType.Application,
            applicationId.Value.ToString(),
            AccessType.Read)
            .Returns(false);

        var handler = new GetApplicationByReferenceQueryHandler(
            applicationRepo,
            httpContextAccessor,
            permissionCheckerService);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to read this application", result.Error);
    }
}