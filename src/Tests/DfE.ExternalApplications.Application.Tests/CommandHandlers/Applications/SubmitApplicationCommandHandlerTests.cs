using System.Security.Claims;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using Microsoft.AspNetCore.Http;
using MockQueryable.NSubstitute;
using NSubstitute;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Applications;

public class SubmitApplicationCommandHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_ShouldSubmitApplication_WhenValidRequestWithAppId(
        SubmitApplicationCommand command,
        User user,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var externalId = "test-app-id";
        var claims = new List<Claim>
        {
            new("appid", externalId)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        // Create a new user with matching external provider ID
        var userWithExternalId = new User(
            user.Id!,
            user.RoleId,
            user.Name,
            user.Email,
            user.CreatedOn,
            user.CreatedBy,
            user.LastModifiedOn,
            user.LastModifiedBy,
            externalId); // Set the external provider ID to match the claim

        var users = new[] { userWithExternalId }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        var applicationId = new ApplicationId(command.ApplicationId);
        var templateVersionId = new TemplateVersionId(Guid.NewGuid());
        var application = new Domain.Entities.Application(
            applicationId,
            "APP-001",
            templateVersionId,
            DateTime.UtcNow,
            userWithExternalId.Id!,
            ApplicationStatus.InProgress); // Not yet submitted

        // Set up the TemplateVersion which is needed for the Submit method
        var templateVersion = new TemplateVersion(
            templateVersionId,
            new TemplateId(Guid.NewGuid()),
            "1.0.0",
            "{}",
            DateTime.UtcNow,
            userWithExternalId.Id!);
        application.GetType().GetProperty("TemplateVersion")?.SetValue(application, templateVersion);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        permissionCheckerService.HasPermission(
            ResourceType.Application,
            command.ApplicationId.ToString(),
            AccessType.Write)
            .Returns(true);

        var handler = new SubmitApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(command.ApplicationId, result.Value.ApplicationId);
        Assert.Equal(ApplicationStatus.Submitted, result.Value.Status);
        Assert.NotNull(result.Value.DateSubmitted);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_ShouldSubmitApplication_WhenValidRequestWithEmail(
        SubmitApplicationCommand command,
        User user,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com"; // Use a known email
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        // Create a user with the exact email that matches the claim
        var testUser = new User(
            user.Id!,
            user.RoleId,
            user.Name,
            email, // Use the same email as in claims
            user.CreatedOn,
            user.CreatedBy,
            user.LastModifiedOn,
            user.LastModifiedBy);

        var users = new[] { testUser }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        var applicationId = new ApplicationId(command.ApplicationId);
        var templateVersionId = new TemplateVersionId(Guid.NewGuid());
        var application = new Domain.Entities.Application(
            applicationId,
            "APP-001",
            templateVersionId,
            DateTime.UtcNow,
            testUser.Id!,
            ApplicationStatus.InProgress); // Not yet submitted

        // Set up the TemplateVersion which is needed for the Submit method
        var templateVersion = new TemplateVersion(
            templateVersionId,
            new TemplateId(Guid.NewGuid()),
            "1.0.0",
            "{}",
            DateTime.UtcNow,
            testUser.Id!);
        application.GetType().GetProperty("TemplateVersion")?.SetValue(application, templateVersion);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        permissionCheckerService.HasPermission(
            ResourceType.Application,
            command.ApplicationId.ToString(),
            AccessType.Write)
            .Returns(true);

        var handler = new SubmitApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(ApplicationStatus.Submitted, result.Value.Status);
        Assert.NotNull(result.Value.DateSubmitted);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserNotAuthenticated(
        SubmitApplicationCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        // Create an unauthenticated ClaimsPrincipal
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        httpContextAccessor.HttpContext.Returns(httpContext);

        // Mock permission checker (not called for unauthenticated users)
        permissionCheckerService.HasPermission(
            Arg.Any<ResourceType>(),
            Arg.Any<string>(),
            Arg.Any<AccessType>())
            .Returns(false);

        var handler = new SubmitApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnApplicationNotFound_WhenApplicationDoesNotExist(
        SubmitApplicationCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IUnitOfWork unitOfWork)
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

        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Test User",
            email,
            DateTime.UtcNow,
            null,      // createdBy
            null,      // lastModifiedOn
            null);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        var applications = Array.Empty<Domain.Entities.Application>().AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        // Mock permission checker to return true so we reach the application lookup
        permissionCheckerService.HasPermission(
            ResourceType.Application,
            command.ApplicationId.ToString(),
            AccessType.Write)
            .Returns(true);

        var handler = new SubmitApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Application not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_ShouldReturnForbidden_WhenUserHasNoPermission(
        SubmitApplicationCommand command,
        User user,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com"; // Use a known email
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        // Create a user with the exact email that matches the claim
        var testUser = new User(
            user.Id!,
            user.RoleId,
            user.Name,
            email, // Use the same email as in claims
            user.CreatedOn,
            user.CreatedBy,
            user.LastModifiedOn,
            user.LastModifiedBy);

        var users = new[] { testUser }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        var applicationId = new ApplicationId(command.ApplicationId);
        var application = new Domain.Entities.Application(
            applicationId,
            "APP-001",
            new TemplateVersionId(Guid.NewGuid()),
            DateTime.UtcNow,
            testUser.Id!,
            ApplicationStatus.InProgress);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        // User has no permission
        permissionCheckerService.HasPermission(
            ResourceType.Application,
            command.ApplicationId.ToString(),
            AccessType.Write)
            .Returns(false);

        var handler = new SubmitApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to submit this application", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_ShouldReturnError_WhenApplicationAlreadySubmitted(
        SubmitApplicationCommand command,
        User user,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com"; // Use a known email
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        // Create a user with the exact email that matches the claim
        var testUser = new User(
            user.Id!,
            user.RoleId,
            user.Name,
            email, // Use the same email as in claims
            user.CreatedOn,
            user.CreatedBy,
            user.LastModifiedOn,
            user.LastModifiedBy);

        var users = new[] { testUser }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        var applicationId = new ApplicationId(command.ApplicationId);
        var application = new Domain.Entities.Application(
            applicationId,
            "APP-001",
            new TemplateVersionId(Guid.NewGuid()),
            DateTime.UtcNow,
            testUser.Id!,
            ApplicationStatus.Submitted); // Already submitted

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        permissionCheckerService.HasPermission(
            ResourceType.Application,
            command.ApplicationId.ToString(),
            AccessType.Write)
            .Returns(true);

        var handler = new SubmitApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Application has already been submitted", result.Error!);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization), typeof(UserCustomization))]
    public async Task Handle_ShouldReturnForbidden_WhenUserIsNotApplicationCreator(
        SubmitApplicationCommand command,
        User user,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com"; // Use a known email
        var claims = new List<Claim>
        {
            new(ClaimTypes.Email, email)
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        // Create a user with the exact email that matches the claim
        var testUser = new User(
            user.Id!,
            user.RoleId,
            user.Name,
            email, // Use the same email as in claims
            user.CreatedOn,
            user.CreatedBy,
            user.LastModifiedOn,
            user.LastModifiedBy);

        var users = new[] { testUser }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        // Create an application that was created by a DIFFERENT user
        var differentUserId = new UserId(Guid.NewGuid());
        var applicationId = new ApplicationId(command.ApplicationId);
        var application = new Domain.Entities.Application(
            applicationId,
            "APP-001",
            new TemplateVersionId(Guid.NewGuid()),
            DateTime.UtcNow,
            differentUserId, // Different user created this application
            ApplicationStatus.InProgress);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        permissionCheckerService.HasPermission(
            ResourceType.Application,
            command.ApplicationId.ToString(),
            AccessType.Write)
            .Returns(true);

        var handler = new SubmitApplicationCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Only the user who created the application can submit it", result.Error);
    }
} 