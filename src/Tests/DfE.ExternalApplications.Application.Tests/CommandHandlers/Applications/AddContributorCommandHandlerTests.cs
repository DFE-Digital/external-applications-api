using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Application.Users.QueryObjects;
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
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;
using Microsoft.EntityFrameworkCore;
using MockQueryable.NSubstitute;
using DfE.ExternalApplications.Domain.Common;
using NSubstitute.ExceptionExtensions;
using System.Reflection;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Applications;

public class AddContributorCommandHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldCreateNewContributor_WhenValidRequest(
        Guid applicationId,
        string contributorName,
        string contributorEmail,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IEaRepository<Role> roleRepo,
        IPermissionCheckerService permissionCheckerService,
        IUserFactory userFactory,
        IUnitOfWork unitOfWork)
    {
        // Use a different email to ensure we create a new contributor, not add permissions to existing user
        var contributorEmailDifferent = "newcontributor@example.com";
        
        // Arrange
        var command = new AddContributorCommand(applicationId, contributorName, contributorEmailDifferent);
        
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
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

        var templateVersionId = new TemplateVersionId(Guid.NewGuid());
        var templateId = new TemplateId(Guid.NewGuid());
        
        var application = new Domain.Entities.Application(
            new ApplicationId(applicationId),
            "APP-001",
            templateVersionId,
            DateTime.UtcNow,
            user.Id!);

        // Set up the TemplateVersion navigation property using reflection
        var templateVersion = new TemplateVersion(
            templateVersionId,
            templateId,
            "1.0.0",
            "{}",
            DateTime.UtcNow,
            user.Id!);
        
        typeof(Domain.Entities.Application)
            .GetProperty(nameof(Domain.Entities.Application.TemplateVersion))!
            .SetValue(application, templateVersion);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        permissionCheckerService.IsApplicationOwner(application, user.Id!.Value.ToString()).Returns(true);
        permissionCheckerService.IsAdmin().Returns(false);

        var contributorId = new UserId(Guid.NewGuid());
        var now = DateTime.UtcNow;
        var contributor = new User(
            contributorId,
            new RoleId(RoleConstants.UserRoleId),
            contributorName,
            contributorEmailDifferent,
            now,
            null,
            null,
            null);

        userFactory.CreateContributor(
            contributorId,
            new RoleId(RoleConstants.UserRoleId),
            contributorName,
            contributorEmailDifferent,
            user.Id!,
            new ApplicationId(applicationId),
            application.TemplateVersion!.TemplateId,
            now).Returns(contributor);

        var handler = new AddContributorCommandHandler(
            applicationRepo,
            userRepo,
            roleRepo,
            httpContextAccessor,
            permissionCheckerService,
            userFactory,
            unitOfWork);

        // Update the mock query to include both the original user and the contributor
        var allUsers = new[] { user, contributor }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(allUsers);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess, $"Result was not successful. Error: {result.Error}");
        Assert.NotNull(result.Value);
        Assert.Equal(contributorId.Value, result.Value.UserId);
        Assert.Equal(contributorName, result.Value.Name);
        Assert.Equal(contributorEmailDifferent, result.Value.Email);
        Assert.Equal(RoleConstants.UserRoleId, result.Value.RoleId);

        await userRepo.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldAddPermissionsToExistingUser_WhenUserExistsButNoPermissions(
        Guid applicationId,
        string contributorName,
        string contributorEmail,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IEaRepository<Role> roleRepo,
        IPermissionCheckerService permissionCheckerService,
        IUserFactory userFactory,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var command = new AddContributorCommand(applicationId, contributorName, contributorEmail);
        
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
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

        var templateVersionId = new TemplateVersionId(Guid.NewGuid());
        var templateId = new TemplateId(Guid.NewGuid());
        
        var application = new Domain.Entities.Application(
            new ApplicationId(applicationId),
            "APP-001",
            templateVersionId,
            DateTime.UtcNow,
            user.Id!);

        // Set up the TemplateVersion navigation property using reflection
        var templateVersion = new TemplateVersion(
            templateVersionId,
            templateId,
            "1.0.0",
            "{}",
            DateTime.UtcNow,
            user.Id!);
        
        typeof(Domain.Entities.Application)
            .GetProperty(nameof(Domain.Entities.Application.TemplateVersion))!
            .SetValue(application, templateVersion);

        // Existing contributor without permissions
        var existingContributor = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(RoleConstants.UserRoleId),
            contributorName,
            contributorEmail,
            DateTime.UtcNow,
            null,
            null,
            null);

        var allUsers = new[] { user, existingContributor }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(allUsers);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        permissionCheckerService.IsApplicationOwner(application, user.Id!.Value.ToString()).Returns(true);
        permissionCheckerService.IsAdmin().Returns(false);

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
        Assert.True(result.IsSuccess, $"Result was not successful. Error: {result.Error}");
        Assert.NotNull(result.Value);
        Assert.Equal(existingContributor.Id!.Value, result.Value.UserId);
        Assert.Equal(contributorName, result.Value.Name);
        Assert.Equal(contributorEmail, result.Value.Email);

        userFactory.Received(1).AddPermissionToUser(
            existingContributor,
            applicationId.ToString(),
            ResourceType.Application,
            Arg.Is<AccessType[]>(a => a.Contains(AccessType.Read) && a.Contains(AccessType.Write)),
            user.Id!,
            new ApplicationId(applicationId));

        userFactory.Received(1).AddTemplatePermissionToUser(
            existingContributor,
            application.TemplateVersion!.TemplateId.Value.ToString(),
            Arg.Is<AccessType[]>(a => a.Contains(AccessType.Read)),
            user.Id!,
            Arg.Any<DateTime>());

        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnExistingContributor_WhenUserExistsWithAllPermissions(
        Guid applicationId,
        string contributorName,
        string contributorEmail,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IEaRepository<Role> roleRepo,
        IPermissionCheckerService permissionCheckerService,
        IUserFactory userFactory,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var command = new AddContributorCommand(applicationId, contributorName, contributorEmail);
        
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "test@example.com";
        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
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

        var application = new ApplicationId(applicationId);
        var templateVersionId = new TemplateVersionId(Guid.NewGuid());
        var templateId = new TemplateId(Guid.NewGuid());
        
        var applicationEntity = new Domain.Entities.Application(
            application,
            "APP-001",
            templateVersionId,
            DateTime.UtcNow,
            user.Id!);

        // Set up the TemplateVersion navigation property using reflection
        var templateVersion = new TemplateVersion(
            templateVersionId,
            templateId,
            "1.0.0",
            "{}",
            DateTime.UtcNow,
            user.Id!);
        
        typeof(Domain.Entities.Application)
            .GetProperty(nameof(Domain.Entities.Application.TemplateVersion))!
            .SetValue(applicationEntity, templateVersion);

        // Existing contributor with all permissions
        var existingContributor = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(RoleConstants.UserRoleId),
            contributorName,
            contributorEmail,
            DateTime.UtcNow,
            null,
            null,
            null);

        // Add permissions using real factory
        var realUserFactory = new UserFactory();
        realUserFactory.AddPermissionToUser(
            existingContributor,
            applicationId.ToString(),
            ResourceType.Application,
            new[] { AccessType.Read, AccessType.Write },
            user.Id!,
            application,
            DateTime.UtcNow);

        var allUsers = new[] { user, existingContributor }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(allUsers);

        var applications = new[] { applicationEntity }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        permissionCheckerService.IsApplicationOwner(applicationEntity, user.Id!.Value.ToString()).Returns(true);
        permissionCheckerService.IsAdmin().Returns(false);

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
        Assert.True(result.IsSuccess, $"Result was not successful. Error: {result.Error}");
        Assert.NotNull(result.Value);
        Assert.Equal(existingContributor.Id!.Value, result.Value.UserId);
        Assert.Equal(contributorName, result.Value.Name);
        Assert.Equal(contributorEmail, result.Value.Email);

        // Should not add new permissions since they already exist
        userFactory.DidNotReceive().AddPermissionToUser(
            Arg.Any<User>(),
            Arg.Any<string>(),
            Arg.Any<ResourceType>(),
            Arg.Any<AccessType[]>(),
            Arg.Any<UserId>(),
            Arg.Any<ApplicationId>());

        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotAuthenticated(
        AddContributorCommand command,
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
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity()); // Not authenticated
        httpContextAccessor.HttpContext.Returns(httpContext);

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
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);

        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenNoUserIdentifier(
        AddContributorCommand command,
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
        var claims = new List<Claim> { new("someclaim", "value") };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

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
        Assert.False(result.IsSuccess);
        Assert.Equal("No user identifier", result.Error);

        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotFound(
        AddContributorCommand command,
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
        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var users = Array.Empty<User>().AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

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
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error);

        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenApplicationNotFound(
        AddContributorCommand command,
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
        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
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

        var applications = Array.Empty<Domain.Entities.Application>().AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

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
        Assert.False(result.IsSuccess);
        Assert.Equal("Application not found", result.Error);

        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenUserNotOwnerOrAdmin(
        AddContributorCommand command,
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
        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
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

        var application = new Domain.Entities.Application(
            new ApplicationId(command.ApplicationId),
            "APP-001",
            new TemplateVersionId(Guid.NewGuid()),
            DateTime.UtcNow,
            user.Id!);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        permissionCheckerService.IsApplicationOwner(application, user.Id!.Value.ToString()).Returns(false);
        permissionCheckerService.IsAdmin().Returns(false);

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
        Assert.False(result.IsSuccess);
        Assert.Equal("Only the application owner or admin can add contributors", result.Error);

        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldAllowAdminToAddContributor(
        Guid applicationId,
        string contributorName,
        string contributorEmail,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IEaRepository<Role> roleRepo,
        IPermissionCheckerService permissionCheckerService,
        IUserFactory userFactory,
        IUnitOfWork unitOfWork)
    {
        var contributorEmailDifferent = "contributor@example.com";

        // Arrange
        var command = new AddContributorCommand(applicationId, contributorName, contributorEmailDifferent);
        
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var email = "admin@example.com";
        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Admin User",
            email,
            DateTime.UtcNow,
            null,
            null,
            null);

        var templateVersionId = new TemplateVersionId(Guid.NewGuid());
        var templateId = new TemplateId(Guid.NewGuid());
        
        var application = new Domain.Entities.Application(
            new ApplicationId(applicationId),
            "APP-001",
            templateVersionId,
            DateTime.UtcNow,
            user.Id!);

        // Set up the TemplateVersion navigation property using reflection
        var templateVersion = new TemplateVersion(
            templateVersionId,
            templateId,
            "1.0.0",
            "{}",
            DateTime.UtcNow,
            user.Id!);
        
        typeof(Domain.Entities.Application)
            .GetProperty(nameof(Domain.Entities.Application.TemplateVersion))!
            .SetValue(application, templateVersion);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        permissionCheckerService.IsApplicationOwner(application, user.Id!.Value.ToString()).Returns(false);
        permissionCheckerService.IsAdmin().Returns(true);

        var contributorId = new UserId(Guid.NewGuid());
        var now = DateTime.UtcNow;
        // Use a different email to ensure we create a new contributor, not add permissions to existing user
        var contributor = new User(
            contributorId,
            new RoleId(RoleConstants.UserRoleId),
            contributorName,
            contributorEmailDifferent,
            now,
            null,
            null,
            null);

        userFactory.CreateContributor(
            contributorId,
            new RoleId(RoleConstants.UserRoleId),
            contributorName,
            contributorEmailDifferent,
            user.Id!,
            new ApplicationId(applicationId),
            application.TemplateVersion!.TemplateId,
            now).Returns(contributor);

        var handler = new AddContributorCommandHandler(
            applicationRepo,
            userRepo,
            roleRepo,
            httpContextAccessor,
            permissionCheckerService,
            userFactory,
            unitOfWork);

        // Set up the mock query to only include the admin user initially
        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        // Set up the mock to update the query after AddAsync is called
        userRepo.When(x => x.AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>()))
            .Do(x => {
                // After AddAsync is called, update the query to include the contributor
                var allUsers = new[] { user, contributor }.AsQueryable().BuildMockDbSet();
                userRepo.Query().Returns(allUsers);
            });

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess, $"Result was not successful. Error: {result.Error}");
        Assert.NotNull(result.Value);
        Assert.Equal(contributorId.Value, result.Value.UserId);

        await userRepo.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldUseExternalProviderId_WhenEmailNotAvailable(
        Guid applicationId,
        string contributorName,
        string contributorEmail,
        IEaRepository<Domain.Entities.Application> applicationRepo,
        IEaRepository<User> userRepo,
        IEaRepository<Role> roleRepo,
        IPermissionCheckerService permissionCheckerService,
        IUserFactory userFactory,
        IUnitOfWork unitOfWork)
    {
        // Arrange
        var command = new AddContributorCommand(applicationId, contributorName, contributorEmail);
        
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = new DefaultHttpContext();
        var externalId = "test@example.com"; // Use email as external provider ID for simplicity
        var claims = new List<Claim> { new("appid", externalId) };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var user = new User(
            new UserId(Guid.NewGuid()),
            new RoleId(Guid.NewGuid()),
            "Test User",
            "test@example.com",
            DateTime.UtcNow,
            null,
            null,
            null,
            externalId);

        var templateVersionId = new TemplateVersionId(Guid.NewGuid());
        var templateId = new TemplateId(Guid.NewGuid());
        
        var application = new Domain.Entities.Application(
            new ApplicationId(applicationId),
            "APP-001",
            templateVersionId,
            DateTime.UtcNow,
            user.Id!);

        // Set up the TemplateVersion navigation property using reflection
        var templateVersion = new TemplateVersion(
            templateVersionId,
            templateId,
            "1.0.0",
            "{}",
            DateTime.UtcNow,
            user.Id!);
        
        typeof(Domain.Entities.Application)
            .GetProperty(nameof(Domain.Entities.Application.TemplateVersion))!
            .SetValue(application, templateVersion);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(users);

        var applications = new[] { application }.AsQueryable().BuildMockDbSet();
        applicationRepo.Query().Returns(applications);

        permissionCheckerService.IsApplicationOwner(application, user.Id!.Value.ToString()).Returns(true);
        permissionCheckerService.IsAdmin().Returns(false);

        var contributorId = new UserId(Guid.NewGuid());
        var now = DateTime.UtcNow;
        var contributor = new User(
            contributorId,
            new RoleId(RoleConstants.UserRoleId),
            contributorName,
            contributorEmail,
            now,
            null,
            null,
            null);

        userFactory.CreateContributor(
            contributorId,
            new RoleId(RoleConstants.UserRoleId),
            contributorName,
            contributorEmail,
            user.Id!,
            new ApplicationId(applicationId),
            application.TemplateVersion!.TemplateId,
            now).Returns(contributor);

        // Update the mock query to include both the original user and the contributor
        var allUsers = new[] { user, contributor }.AsQueryable().BuildMockDbSet();
        userRepo.Query().Returns(allUsers);

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
        Assert.True(result.IsSuccess, $"Result was not successful. Error: {result.Error}");
        Assert.NotNull(result.Value);
        Assert.Equal(contributorId.Value, result.Value.UserId);

        await userRepo.Received(1).AddAsync(Arg.Any<User>(), Arg.Any<CancellationToken>());
        await unitOfWork.Received(1).CommitAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public async Task Handle_ShouldReturnFailure_WhenExceptionOccurs(
        AddContributorCommand command,
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
        var claims = new List<Claim> { new(ClaimTypes.Email, email) };
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

        // Simulate an exception during query execution
        userRepo.Query().Throws(new InvalidOperationException("Database connection failed"));

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
        Assert.False(result.IsSuccess);
        Assert.Equal("Database connection failed", result.Error);

        await unitOfWork.DidNotReceive().CommitAsync(Arg.Any<CancellationToken>());
    }
} 