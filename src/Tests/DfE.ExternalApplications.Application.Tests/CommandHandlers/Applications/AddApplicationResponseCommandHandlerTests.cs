using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Applications;

public class AddApplicationResponseCommandHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldAddResponseVersion_WhenValidRequest(
        Guid applicationId,
        string responseBody,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IApplicationFactory applicationFactory,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var encodedBody = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(responseBody));
        var command = new AddApplicationResponseCommand(applicationId, encodedBody);
        
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", email, DateTime.UtcNow, null, null, null);
        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.HasPermission(ResourceType.Application, command.ApplicationId.ToString(), AccessType.Write).Returns(true);

        var appDomainId = new ApplicationId(command.ApplicationId);
        var application = new Domain.Entities.Application(appDomainId, "APP-001", new TemplateVersionId(Guid.NewGuid()), DateTime.UtcNow, user.Id!);
        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);
        
        var newResponse = new ApplicationResponse(new ResponseId(Guid.NewGuid()), appDomainId, responseBody, DateTime.UtcNow, user.Id!);

        applicationFactory.AddResponseToApplication(application, responseBody, user.Id!).Returns(newResponse);

        var handler = new AddApplicationResponseCommandHandler(applicationRepo, userRepo, httpContextAccessor, permissionCheckerService, applicationFactory, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(command.ApplicationId, result.Value.ApplicationId);
        Assert.Equal(responseBody, result.Value.ResponseBody);
        Assert.Equal(user.Id!.Value, result.Value.CreatedBy);
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldAddResponseVersion_WhenValidRequestWithExternalId(
        Guid applicationId,
        string responseBody,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IApplicationFactory applicationFactory,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var encodedBody = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(responseBody));
        var command = new AddApplicationResponseCommand(applicationId, encodedBody);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var externalId = "external-id";
        var claims = new List<Claim> { new("appid", externalId), new(ClaimTypes.Email, "test@example.com") };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null, externalId);
        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.HasPermission(ResourceType.Application, command.ApplicationId.ToString(), AccessType.Write).Returns(true);

        var appDomainId = new ApplicationId(command.ApplicationId);
        var application = new Domain.Entities.Application(appDomainId, "APP-001", new TemplateVersionId(Guid.NewGuid()), DateTime.UtcNow, user.Id!);
        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        var newResponse = new ApplicationResponse(new ResponseId(Guid.NewGuid()), appDomainId, responseBody, DateTime.UtcNow, user.Id!);

        applicationFactory.AddResponseToApplication(application, responseBody, user.Id!).Returns(newResponse);

        var handler = new AddApplicationResponseCommandHandler(applicationRepo, userRepo, httpContextAccessor, permissionCheckerService, applicationFactory, unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(command.ApplicationId, result.Value.ApplicationId);
        Assert.Equal(responseBody, result.Value.ResponseBody);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotAuthenticated(
        AddApplicationResponseCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IApplicationFactory applicationFactory,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new AddApplicationResponseCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            applicationFactory,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound(
        AddApplicationResponseCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IApplicationFactory applicationFactory,
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

        var users = Array.Empty<User>().AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        var handler = new AddApplicationResponseCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            applicationFactory,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserLacksPermission(
        AddApplicationResponseCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IApplicationFactory applicationFactory,
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
            null,
            null,
            null);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.HasPermission(ResourceType.Application, command.ApplicationId.ToString(), AccessType.Write).Returns(false);

        var handler = new AddApplicationResponseCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            applicationFactory,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to update this application", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenApplicationNotFound(
        AddApplicationResponseCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IApplicationFactory applicationFactory,
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
            null,
            null,
            null);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.HasPermission(ResourceType.Application, command.ApplicationId.ToString(), AccessType.Write).Returns(true);

        var applications = Array.Empty<Domain.Entities.Application>().AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        var handler = new AddApplicationResponseCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            applicationFactory,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Application not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenBodyIsInvalidBase64(
        Guid applicationId,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IApplicationFactory applicationFactory,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var command = new AddApplicationResponseCommand(applicationId, "this is not base64");
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", email, DateTime.UtcNow, null, null, null);
        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.HasPermission(ResourceType.Application, command.ApplicationId.ToString(), AccessType.Write).Returns(true);

        var application = new Domain.Entities.Application(
            new ApplicationId(command.ApplicationId),
            "APP-001",
            new TemplateVersionId(Guid.NewGuid()),
            DateTime.UtcNow,
            user.Id!);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        var handler = new AddApplicationResponseCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            applicationFactory,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid Base64 format for ResponseBody", result.Error);
    }
} 