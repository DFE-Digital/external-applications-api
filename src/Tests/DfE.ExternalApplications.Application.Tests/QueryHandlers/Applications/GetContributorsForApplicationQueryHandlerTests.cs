using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using MediatR;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Applications;

public class GetContributorsForApplicationQueryHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnContributors_WhenValidRequest(
        GetContributorsForApplicationQuery query,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
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

        permissionCheckerService.HasPermission(ResourceType.Application, query.ApplicationId.ToString(), AccessType.Read).Returns(true);

        var application = new Domain.Entities.Application(
            new ApplicationId(query.ApplicationId),
            "APP-001",
            new TemplateVersionId(Guid.NewGuid()),
            DateTime.UtcNow,
            user.Id!);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        // Contributors with permissions for this application
        var contributor1 = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Contributor 1",
            "contributor1@example.com",
            DateTime.UtcNow,
            null,
            null,
            null);

        var contributor2 = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Contributor 2",
            "contributor2@example.com",
            DateTime.UtcNow,
            null,
            null,
            null);

        // Add permissions for this application
        contributor1.AddPermission(
            new ApplicationId(query.ApplicationId),
            query.ApplicationId.ToString(),
            ResourceType.Application,
            AccessType.Read,
            user.Id!);

        contributor2.AddPermission(
            new ApplicationId(query.ApplicationId),
            query.ApplicationId.ToString(),
            ResourceType.Application,
            AccessType.Read,
            user.Id!);

        var contributorUsers = new[] { contributor1, contributor2 }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(contributorUsers);

        var handler = new GetContributorsForApplicationQueryHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, c => c.Name == "Contributor 1");
        Assert.Contains(result.Value, c => c.Name == "Contributor 2");
        
        // Check that Authorization data is populated
        foreach (var contributor in result.Value)
        {
            Assert.NotNull(contributor.Authorization);
            Assert.NotNull(contributor.Authorization.Permissions);
            Assert.NotNull(contributor.Authorization.Roles);
            Assert.NotEmpty(contributor.Authorization.Permissions);
            Assert.NotEmpty(contributor.Authorization.Roles);
        }
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnEmptyList_WhenNoContributors(
        GetContributorsForApplicationQuery query,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
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

        permissionCheckerService.HasPermission(ResourceType.Application, query.ApplicationId.ToString(), AccessType.Read).Returns(true);

        var application = new Domain.Entities.Application(
            new ApplicationId(query.ApplicationId),
            "APP-001",
            new TemplateVersionId(Guid.NewGuid()),
            DateTime.UtcNow,
            user.Id!);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        // No contributors
        var contributorUsers = new List<User>().AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(contributorUsers);

        var handler = new GetContributorsForApplicationQueryHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotAuthenticated(
        GetContributorsForApplicationQuery query,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new GetContributorsForApplicationQueryHandler(
            applicationRepo,
            userRepo,
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
    public async Task Handle_ShouldReturnFailure_WhenNoUserIdentifier(
        GetContributorsForApplicationQuery query,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim>
        {
            new("someclaim", "value")
        };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new GetContributorsForApplicationQueryHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No user identifier", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound(
        GetContributorsForApplicationQuery query,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
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

        var users = new List<User>().AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        var handler = new GetContributorsForApplicationQueryHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserDoesNotHavePermission(
        GetContributorsForApplicationQuery query,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
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

        permissionCheckerService.HasPermission(ResourceType.Application, query.ApplicationId.ToString(), AccessType.Read).Returns(false);

        var handler = new GetContributorsForApplicationQueryHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to read this application", result.Error);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenApplicationNotFound(
        GetContributorsForApplicationQuery query,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
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

        permissionCheckerService.HasPermission(ResourceType.Application, query.ApplicationId.ToString(), AccessType.Read).Returns(true);

        var applications = new List<Domain.Entities.Application>().AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        var handler = new GetContributorsForApplicationQueryHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Application not found", result.Error);
    }
} 