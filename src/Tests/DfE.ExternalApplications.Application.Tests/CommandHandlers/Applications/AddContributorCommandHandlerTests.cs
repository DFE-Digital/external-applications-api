using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Users.QueryObjects;
using DfE.ExternalApplications.Domain.Common;
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
using MediatR;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Applications;

public class AddContributorCommandHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldAddNewContributor_WhenValidRequest(
        AddContributorCommand command,
        Role defaultRole,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IEaRepository<Role> roleRepo,
        IPermissionCheckerService permissionCheckerService,
        IUserFactory userFactory,
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

        permissionCheckerService.IsApplicationOwner(Arg.Any<Domain.Entities.Application>(), user.Id!.Value.ToString()).Returns(true);
        permissionCheckerService.IsAdmin().Returns(false);

        var application = new Domain.Entities.Application(
            new ApplicationId(command.ApplicationId),
            "APP-001",
            new TemplateVersionId(Guid.NewGuid()),
            DateTime.UtcNow,
            user.Id!);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        // No existing contributor
        var existingUsers = new List<User>().AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(existingUsers);

        defaultRole.Id = new RoleId(RoleConstants.UserRoleId);
        defaultRole.Name = "User";
        var roles = new[] { defaultRole }.AsQueryable().BuildMockDbSet();
        roleRepo.Query().Returns(roles);

        // Mock the factory to return a contributor
        var contributor = new User(
            new UserId(Guid.NewGuid()),
            defaultRole.Id!,
            command.Name,
            command.Email,
            DateTime.UtcNow,
            user.Id!,
            null,
            null);
        userFactory.CreateContributor(
            Arg.Any<UserId>(),
            Arg.Any<RoleId>(),
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Any<UserId>(),
            Arg.Any<ApplicationId>(),
            Arg.Any<DateTime?>())
            .Returns(contributor);

        var handler = new AddContributorCommandHandler(
            applicationRepo,
            userRepo,
            roleRepo,
            httpContextAccessor,
            permissionCheckerService,
            userFactory,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(command.Name, result.Value.Name);
        Assert.Equal(command.Email, result.Value.Email);
        Assert.Equal(defaultRole.Id!.Value, result.Value.RoleId);

        await userRepo.Received(1)
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1)
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldAddPermissionToExistingUser_WhenUserExists(
        AddContributorCommand command,
        Role defaultRole,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IEaRepository<Role> roleRepo,
        IPermissionCheckerService permissionCheckerService,
        IUserFactory userFactory,
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

        // Existing contributor without permission for this application
        var existingContributor = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            command.Name,
            command.Email,
            DateTime.UtcNow,
            null,
            null,
            null);

        var existingUsers = new[] { existingContributor }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(existingUsers);

        var handler = new AddContributorCommandHandler(
            applicationRepo,
            userRepo,
            roleRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(command.Name, result.Value.Name);
        Assert.Equal(command.Email, result.Value.Email);

        await unitOfWork.Received(1)
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotAuthenticated(
        AddContributorCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IEaRepository<Role> roleRepo,
        IPermissionCheckerService permissionCheckerService,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new AddContributorCommandHandler(
            applicationRepo,
            userRepo,
            roleRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);

        await userRepo.DidNotReceive()
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenNoUserIdentifier(
        AddContributorCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IEaRepository<Role> roleRepo,
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

        var handler = new AddContributorCommandHandler(
            applicationRepo,
            userRepo,
            roleRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("No user identifier", result.Error);

        await userRepo.DidNotReceive()
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound(
        AddContributorCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IEaRepository<Role> roleRepo,
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

        var handler = new AddContributorCommandHandler(
            applicationRepo,
            userRepo,
            roleRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);

        await userRepo.DidNotReceive()
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserDoesNotHavePermission(
        AddContributorCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IEaRepository<Role> roleRepo,
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

        var handler = new AddContributorCommandHandler(
            applicationRepo,
            userRepo,
            roleRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User does not have permission to manage contributors for this application", result.Error);

        await userRepo.DidNotReceive()
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenApplicationNotFound(
        AddContributorCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IEaRepository<Role> roleRepo,
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

        var handler = new AddContributorCommandHandler(
            applicationRepo,
            userRepo,
            roleRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Application not found", result.Error);

        await userRepo.DidNotReceive()
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenContributorAlreadyExists(
        AddContributorCommand command,
        Role defaultRole,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IEaRepository<Role> roleRepo,
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

        // Existing contributor with permission for this application
        var existingContributor = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            command.Name,
            command.Email,
            DateTime.UtcNow,
            null,
            null,
            null);

        var permission = new Permission(
            new PermissionId(Guid.NewGuid()),
            existingContributor.Id!,
            new ApplicationId(command.ApplicationId),
            command.ApplicationId.ToString(),
            ResourceType.Application,
            AccessType.Read,
            DateTime.UtcNow,
            user.Id!);

        existingContributor = new User(
            existingContributor.Id!,
            existingContributor.RoleId,
            existingContributor.Name,
            existingContributor.Email,
            existingContributor.CreatedOn,
            existingContributor.CreatedBy,
            existingContributor.LastModifiedOn,
            existingContributor.LastModifiedBy,
            existingContributor.ExternalProviderId,
            new[] { permission });

        var existingUsers = new[] { existingContributor }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(existingUsers);

        var handler = new AddContributorCommandHandler(
            applicationRepo,
            userRepo,
            roleRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Contributor already exists for this application", result.Error);

        await userRepo.DidNotReceive()
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenDefaultRoleNotFound(
        AddContributorCommand command,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IEaRepository<Role> roleRepo,
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

        // No existing contributor
        var existingUsers = new List<User>().AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(existingUsers);

        // No default role
        var roles = new List<Role>().AsQueryable().BuildMockDbSet();
        roleRepo.Query().Returns(roles);

        var handler = new AddContributorCommandHandler(
            applicationRepo,
            userRepo,
            roleRepo,
            httpContextAccessor,
            permissionCheckerService,
            unitOfWork);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Default user role not found", result.Error);

        await userRepo.DidNotReceive()
            .AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive()
            .CommitAsync(Arg.Any<CancellationToken>());
    }
} 