using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Domain.Factories;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using MockQueryable.NSubstitute;
using NSubstitute;
using Xunit;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Applications;

public class AddContributorCommandHandlerTests
{
    private readonly AddContributorCommandHandler _handler;
    private readonly IEaRepository<Domain.Entities.Application> _applicationRepo;
    private readonly IEaRepository<User> _userRepo;
    private readonly IEaRepository<Role> _roleRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionCheckerService _permissionCheckerService;
    private readonly IUserFactory _userFactory;
    private readonly IUnitOfWork _unitOfWork;
    private readonly HttpContext _httpContext;
    private readonly ClaimsPrincipal _user;

    public AddContributorCommandHandlerTests()
    {
        _applicationRepo = Substitute.For<IEaRepository<Domain.Entities.Application>>();
        _userRepo = Substitute.For<IEaRepository<User>>();
        _roleRepo = Substitute.For<IEaRepository<Role>>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _permissionCheckerService = Substitute.For<IPermissionCheckerService>();
        _userFactory = Substitute.For<IUserFactory>();
        _unitOfWork = Substitute.For<IUnitOfWork>();
        
        _httpContext = Substitute.For<HttpContext>();
        _user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, "test@example.com")
        }));
        _httpContext.User.Returns(_user);
        _httpContextAccessor.HttpContext.Returns(_httpContext);
        
        _handler = new AddContributorCommandHandler(
            _applicationRepo,
            _userRepo,
            _roleRepo,
            _httpContextAccessor,
            _permissionCheckerService,
            _userFactory,
            _unitOfWork);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldAddContributorSuccessfully()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var command = new AddContributorCommand(applicationId, "John Doe", "newuser@example.com");

        var dbUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var application = new Domain.Entities.Application(new ApplicationId(applicationId), "APP-001", new TemplateVersionId(Guid.NewGuid()), DateTime.UtcNow, dbUser.Id!);
        var contributor = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "John Doe", "newuser@example.com", DateTime.UtcNow, dbUser.Id!, null, null);

        var usersQuery = new[] { dbUser }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        var applicationsQuery = new[] { application }.AsQueryable().BuildMockDbSet();
        _applicationRepo.Query().Returns(applicationsQuery);

        // No existing contributor
        var existingUsersQuery = new List<User>().AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(existingUsersQuery);

        _permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString()).Returns(true);
        _permissionCheckerService.IsAdmin().Returns(false);

        _userFactory.CreateContributor(Arg.Any<UserId>(), Arg.Any<RoleId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UserId>(), Arg.Any<ApplicationId>(), Arg.Any<DateTime?>())
            .Returns(contributor);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(contributor.Id!.Value, result.Value.UserId);
        Assert.Equal(contributor.Name, result.Value.Name);
        Assert.Equal(contributor.Email, result.Value.Email);
    }

    [Fact]
    public async Task Handle_WhenApplicationNotFound_ShouldReturnFailure()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var command = new AddContributorCommand(applicationId, "John Doe", "newuser@example.com");

        var dbUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);

        var usersQuery = new[] { dbUser }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        // No application found
        var applicationsQuery = new List<Domain.Entities.Application>().AsQueryable().BuildMockDbSet();
        _applicationRepo.Query().Returns(applicationsQuery);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Application not found", result.Error);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldReturnFailure()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var command = new AddContributorCommand(applicationId, "John Doe", "newuser@example.com");

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns((ClaimsPrincipal?)null);
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new AddContributorCommandHandler(
            _applicationRepo,
            _userRepo,
            _roleRepo,
            httpContextAccessor,
            _permissionCheckerService,
            _userFactory,
            _unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Not authenticated", result.Error);
    }

    [Fact]
    public async Task Handle_WhenNoUserIdentifier_ShouldReturnFailure()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var command = new AddContributorCommand(applicationId, "John Doe", "newuser@example.com");

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("someclaim", "value") // No email or appid claim
        }));
        _httpContext.User.Returns(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("No user identifier", result.Error);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var command = new AddContributorCommand(applicationId, "John Doe", "newuser@example.com");

        // No users found
        var usersQuery = new List<User>().AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("User not found", result.Error);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthorized_ShouldReturnFailure()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var command = new AddContributorCommand(applicationId, "John Doe", "newuser@example.com");

        var dbUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var application = new Domain.Entities.Application(new ApplicationId(applicationId), "APP-001", new TemplateVersionId(Guid.NewGuid()), DateTime.UtcNow, dbUser.Id!);

        var usersQuery = new[] { dbUser }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        var applicationsQuery = new[] { application }.AsQueryable().BuildMockDbSet();
        _applicationRepo.Query().Returns(applicationsQuery);

        _permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString()).Returns(false);
        _permissionCheckerService.IsAdmin().Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Only the application owner or admin can add contributors", result.Error);
    }

    [Fact]
    public async Task Handle_WhenContributorAlreadyExists_ShouldReturnExistingContributor()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var command = new AddContributorCommand(applicationId, "John Doe", "existing@example.com");

        var dbUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var application = new Domain.Entities.Application(new ApplicationId(applicationId), "APP-001", new TemplateVersionId(Guid.NewGuid()), DateTime.UtcNow, dbUser.Id!);
        var existingContributor = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "John Doe", "existing@example.com", DateTime.UtcNow, null, null, null);

        var usersQuery = new[] { dbUser }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        var applicationsQuery = new[] { application }.AsQueryable().BuildMockDbSet();
        _applicationRepo.Query().Returns(applicationsQuery);

        // Existing contributor with permissions
        var existingUsersQuery = new[] { existingContributor }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(existingUsersQuery);

        _permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString()).Returns(true);
        _permissionCheckerService.IsAdmin().Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(existingContributor.Id!.Value, result.Value.UserId);
        Assert.Equal(existingContributor.Name, result.Value.Name);
        Assert.Equal(existingContributor.Email, result.Value.Email);
    }

    [Fact]
    public async Task Handle_WhenUnitOfWorkFails_ShouldReturnFailure()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var command = new AddContributorCommand(applicationId, "John Doe", "newuser@example.com");

        var dbUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var application = new Domain.Entities.Application(new ApplicationId(applicationId), "APP-001", new TemplateVersionId(Guid.NewGuid()), DateTime.UtcNow, dbUser.Id!);
        var contributor = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "John Doe", "newuser@example.com", DateTime.UtcNow, dbUser.Id!, null, null);

        var usersQuery = new[] { dbUser }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        var applicationsQuery = new[] { application }.AsQueryable().BuildMockDbSet();
        _applicationRepo.Query().Returns(applicationsQuery);

        // No existing contributor
        var existingUsersQuery = new List<User>().AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(existingUsersQuery);

        _permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString()).Returns(true);
        _permissionCheckerService.IsAdmin().Returns(false);

        _userFactory.CreateContributor(Arg.Any<UserId>(), Arg.Any<RoleId>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<UserId>(), Arg.Any<ApplicationId>(), Arg.Any<DateTime?>())
            .Returns(contributor);

        _unitOfWork.CommitAsync(Arg.Any<CancellationToken>()).Returns(Task.FromException<int>(new Exception("Database error")));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Database error", result.Error);
    }
} 