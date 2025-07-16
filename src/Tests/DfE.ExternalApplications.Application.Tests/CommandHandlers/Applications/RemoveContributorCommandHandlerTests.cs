using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Commands;
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

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Applications;

public class RemoveContributorCommandHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldRemoveContributor_WhenValidRequest(
        RemoveContributorCommand command,
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
            null,
            null,
            null);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.CanManageContributors(command.ApplicationId.ToString()).Returns(true);

        var application = new Domain.Entities.Application(
            new ApplicationId(command.ApplicationId),
            "APP-001",
            new TemplateVersionId(Guid.NewGuid()),
            DateTime.UtcNow,
            user.Id!);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        // Contributor with permission for this application
        var contributor = new User(
            new UserId(command.UserId),
            new RoleId(Guid.NewGuid()),
            "Contributor User",
            "contributor@example.com",
            DateTime.UtcNow,
            null,
            null,
            null);

        // Add permission for this application
        contributor.AddPermission(
            new ApplicationId(command.ApplicationId),
            command.ApplicationId.ToString(),
            ResourceType.Application,
            AccessType.Read,
            user.Id!);

        var contributorUsers = new[] { contributor }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(contributorUsers);

        var handler = new RemoveContributorCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.True(result.Value);

        await unitOfWork.Received(1)
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotAuthenticated(
        RemoveContributorCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new RemoveContributorCommandHandler(
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

        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenNoUserIdentifier(
        RemoveContributorCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IPermissionCheckerService permissionCheckerService,
        IUnitOfWork unitOfWork)
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

        var handler = new RemoveContributorCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No user identifier", result.Error);

        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound(
        RemoveContributorCommand command,
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

        var users = new List<User>().AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        var handler = new RemoveContributorCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);

        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserDoesNotHavePermission(
        RemoveContributorCommand command,
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
            null,
            null,
            null);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.CanManageContributors(command.ApplicationId.ToString()).Returns(false);

        var handler = new RemoveContributorCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to manage contributors for this application", result.Error);

        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenApplicationNotFound(
        RemoveContributorCommand command,
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
            null,
            null,
            null);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.CanManageContributors(command.ApplicationId.ToString()).Returns(true);

        var applications = new List<Domain.Entities.Application>().AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        var handler = new RemoveContributorCommandHandler(
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

        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenContributorNotFound(
        RemoveContributorCommand command,
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
            null,
            null,
            null);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.CanManageContributors(command.ApplicationId.ToString()).Returns(true);

        var application = new Domain.Entities.Application(
            new ApplicationId(command.ApplicationId),
            "APP-001",
            new TemplateVersionId(Guid.NewGuid()),
            DateTime.UtcNow,
            user.Id!);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        // No contributor found
        var contributorUsers = new List<User>().AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(contributorUsers);

        var handler = new RemoveContributorCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Contributor not found", result.Error);

        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenContributorDoesNotHavePermission(
        RemoveContributorCommand command,
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
            null,
            null,
            null);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        permissionCheckerService.CanManageContributors(command.ApplicationId.ToString()).Returns(true);

        var application = new Domain.Entities.Application(
            new ApplicationId(command.ApplicationId),
            "APP-001",
            new TemplateVersionId(Guid.NewGuid()),
            DateTime.UtcNow,
            user.Id!);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        // Contributor without permission for this application
        var contributor = new User(
            new UserId(command.UserId),
            new RoleId(Guid.NewGuid()),
            "Contributor User",
            "contributor@example.com",
            DateTime.UtcNow,
            null,
            null,
            null);

        var contributorUsers = new[] { contributor }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(contributorUsers);

        var handler = new RemoveContributorCommandHandler(
            applicationRepo,
            userRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Contributor does not have permission for this application", result.Error);

        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }
} 