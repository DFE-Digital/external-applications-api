using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Application.Applications.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using MockQueryable.NSubstitute;
using NSubstitute;
using System.Security.Claims;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Applications;

public class GetContributorsForApplicationQueryHandlerTests
{
    private readonly GetContributorsForApplicationQueryHandler _handler;
    private readonly IEaRepository<Domain.Entities.Application> _applicationRepo;
    private readonly IEaRepository<User> _userRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IPermissionCheckerService _permissionCheckerService;
    private readonly HttpContext _httpContext;
    private readonly ClaimsPrincipal _user;

    public GetContributorsForApplicationQueryHandlerTests()
    {
        _applicationRepo = Substitute.For<IEaRepository<Domain.Entities.Application>>();
        _userRepo = Substitute.For<IEaRepository<User>>();
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _permissionCheckerService = Substitute.For<IPermissionCheckerService>();
        
        _httpContext = Substitute.For<HttpContext>();
        _user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("appid", "test-user-id")
        }, "Test"));
        _httpContext.User.Returns(_user);
        _httpContextAccessor.HttpContext.Returns(_httpContext);
        
        _handler = new GetContributorsForApplicationQueryHandler(
            _applicationRepo,
            _userRepo,
            _httpContextAccessor,
            _permissionCheckerService);
    }

    [Fact]
    public async Task Handle_WithValidRequest_ShouldReturnContributors()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var query = new GetContributorsForApplicationQuery(applicationId, true);

        var dbUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null, "test-user-id");
        var application = new Domain.Entities.Application(new ApplicationId(applicationId), "APP-001", new TemplateVersionId(Guid.NewGuid()), DateTime.UtcNow, dbUser.Id!);

        // Create a contributor user with permissions for the application
        var contributorUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Contributor User", "contributor@example.com", DateTime.UtcNow, null, null, null);
        
        // Add permission to the contributor user using reflection since AddPermission is internal
        var permission = new Permission(
            new PermissionId(Guid.NewGuid()),
            contributorUser.Id,
            new ApplicationId(applicationId),
            "Application:Read",
            ResourceType.Application,
            AccessType.Read,
            DateTime.UtcNow,
            dbUser.Id!);
        
        var permissionsField = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var permissions = new List<Permission> { permission };
        permissionsField!.SetValue(contributorUser, permissions);

        // Set up role for contributor
        var role = new Role(contributorUser.RoleId, "Contributor");
        var roleProperty = typeof(User).GetProperty("Role");
        roleProperty!.SetValue(contributorUser, role);

        var usersQuery = new[] { dbUser, contributorUser }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        var applicationsQuery = new[] { application }.AsQueryable().BuildMockDbSet();
        _applicationRepo.Query().Returns(applicationsQuery);

        _permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString()).Returns(true);
        _permissionCheckerService.IsAdmin().Returns(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal(contributorUser.Id!.Value, result.Value.First().UserId);
    }

    [Fact]
    public async Task Handle_WhenApplicationNotFound_ShouldReturnFailure()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var query = new GetContributorsForApplicationQuery(applicationId, false);

        var dbUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null, "test-user-id");

        var usersQuery = new[] { dbUser }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        // No application found
        var applicationsQuery = new List<Domain.Entities.Application>().AsQueryable().BuildMockDbSet();
        _applicationRepo.Query().Returns(applicationsQuery);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Application not found", result.Error);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldReturnFailure()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var query = new GetContributorsForApplicationQuery(applicationId, false);

        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var httpContext = Substitute.For<HttpContext>();
        httpContext.User.Returns((ClaimsPrincipal?)null);
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new GetContributorsForApplicationQueryHandler(
            _applicationRepo,
            _userRepo,
            httpContextAccessor,
            _permissionCheckerService);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Not authenticated", result.Error);
    }

    [Fact]
    public async Task Handle_WhenNoUserIdentifier_ShouldReturnFailure()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var query = new GetContributorsForApplicationQuery(applicationId, false);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("someclaim", "value") // No appid, azp, or email claim
        }, "Test"));
        _httpContext.User.Returns(user);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("No user identifier", result.Error);
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ShouldReturnFailure()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var query = new GetContributorsForApplicationQuery(applicationId, false);

        // No users found
        var usersQuery = new List<User>().AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("User not found", result.Error);
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthorized_ShouldReturnFailure()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var query = new GetContributorsForApplicationQuery(applicationId, false);

        var dbUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null, "test-user-id");
        var application = new Domain.Entities.Application(new ApplicationId(applicationId), "APP-001", new TemplateVersionId(Guid.NewGuid()), DateTime.UtcNow, dbUser.Id!);

        var usersQuery = new[] { dbUser }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        var applicationsQuery = new[] { application }.AsQueryable().BuildMockDbSet();
        _applicationRepo.Query().Returns(applicationsQuery);

        _permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString()).Returns(false);
        _permissionCheckerService.IsAdmin().Returns(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Only the application owner or admin can view contributors", result.Error);
    }

    [Fact]
    public async Task Handle_WhenUserIsAdmin_ShouldReturnContributors()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var query = new GetContributorsForApplicationQuery(applicationId, false);

        var dbUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null, "test-user-id");
        var application = new Domain.Entities.Application(new ApplicationId(applicationId), "APP-001", new TemplateVersionId(Guid.NewGuid()), DateTime.UtcNow, dbUser.Id!);

        // Create a contributor user with permissions for the application
        var contributorUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Contributor User", "contributor@example.com", DateTime.UtcNow, null, null, null);
        
        // Add permission to the contributor user using reflection since AddPermission is internal
        var permission = new Permission(
            new PermissionId(Guid.NewGuid()),
            contributorUser.Id,
            new ApplicationId(applicationId),
            "Application:Read",
            ResourceType.Application,
            AccessType.Read,
            DateTime.UtcNow,
            dbUser.Id!);
        
        var permissionsField = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var permissions = new List<Permission> { permission };
        permissionsField!.SetValue(contributorUser, permissions);

        // Set up role for contributor
        var role = new Role(contributorUser.RoleId, "Contributor");
        var roleProperty = typeof(User).GetProperty("Role");
        roleProperty!.SetValue(contributorUser, role);

        var usersQuery = new[] { dbUser, contributorUser }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        var applicationsQuery = new[] { application }.AsQueryable().BuildMockDbSet();
        _applicationRepo.Query().Returns(applicationsQuery);

        _permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString()).Returns(false);
        _permissionCheckerService.IsAdmin().Returns(true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        Assert.Equal(contributorUser.Id!.Value, result.Value.First().UserId);
    }

    [Fact]
    public async Task Handle_WhenQueryObjectFails_ShouldReturnFailure()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var query = new GetContributorsForApplicationQuery(applicationId, false);

        var dbUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null, "test-user-id");
        var application = new Domain.Entities.Application(new ApplicationId(applicationId), "APP-001", new TemplateVersionId(Guid.NewGuid()), DateTime.UtcNow, dbUser.Id!);

        var usersQuery = new[] { dbUser }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        var applicationsQuery = new[] { application }.AsQueryable().BuildMockDbSet();
        _applicationRepo.Query().Returns(applicationsQuery);

        _permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString()).Returns(true);
        _permissionCheckerService.IsAdmin().Returns(false);

        // Simulate query object failure by throwing an exception
        _userRepo.Query().Returns(x => { throw new Exception("Database error"); });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Contains("Database error", result.Error);
    }

    [Fact]
    public async Task Handle_WithEmailClaim_ShouldFindUserByEmail()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var query = new GetContributorsForApplicationQuery(applicationId, false);

        var dbUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var application = new Domain.Entities.Application(new ApplicationId(applicationId), "APP-001", new TemplateVersionId(Guid.NewGuid()), DateTime.UtcNow, dbUser.Id!);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, "test@example.com")
        }, "Test"));
        _httpContext.User.Returns(user);

        var usersQuery = new[] { dbUser }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        var applicationsQuery = new[] { application }.AsQueryable().BuildMockDbSet();
        _applicationRepo.Query().Returns(applicationsQuery);

        _permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString()).Returns(true);
        _permissionCheckerService.IsAdmin().Returns(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value); // No contributors for this application
    }

    [Fact]
    public async Task Handle_WithAzpClaim_ShouldFindUserByExternalProviderId()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var query = new GetContributorsForApplicationQuery(applicationId, false);

        var dbUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null, "external-provider-id");
        var application = new Domain.Entities.Application(new ApplicationId(applicationId), "APP-001", new TemplateVersionId(Guid.NewGuid()), DateTime.UtcNow, dbUser.Id!);

        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("azp", "external-provider-id")
        }, "Test"));
        _httpContext.User.Returns(user);

        var usersQuery = new[] { dbUser }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        var applicationsQuery = new[] { application }.AsQueryable().BuildMockDbSet();
        _applicationRepo.Query().Returns(applicationsQuery);

        _permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString()).Returns(true);
        _permissionCheckerService.IsAdmin().Returns(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value); // No contributors for this application
    }

    [Fact]
    public async Task Handle_WithIncludePermissionDetails_ShouldReturnAuthorizationData()
    {
        // Arrange
        var applicationId = Guid.NewGuid();
        var query = new GetContributorsForApplicationQuery(applicationId, true); // Include permission details

        var dbUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null, "test-user-id");
        var application = new Domain.Entities.Application(new ApplicationId(applicationId), "APP-001", new TemplateVersionId(Guid.NewGuid()), DateTime.UtcNow, dbUser.Id!);

        // Create a contributor user with permissions for the application
        var contributorUser = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Contributor User", "contributor@example.com", DateTime.UtcNow, null, null, null);
        
        // Add permission to the contributor user using reflection since AddPermission is internal
        var permission = new Permission(
            new PermissionId(Guid.NewGuid()),
            contributorUser.Id,
            new ApplicationId(applicationId),
            "Application:Read",
            ResourceType.Application,
            AccessType.Read,
            DateTime.UtcNow,
            dbUser.Id!);
        
        var permissionsField = typeof(User).GetField("_permissions", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var permissions = new List<Permission> { permission };
        permissionsField!.SetValue(contributorUser, permissions);

        // Set up role for contributor
        var role = new Role(contributorUser.RoleId, "Contributor");
        var roleProperty = typeof(User).GetProperty("Role");
        roleProperty!.SetValue(contributorUser, role);

        var usersQuery = new[] { dbUser, contributorUser }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(usersQuery);

        var applicationsQuery = new[] { application }.AsQueryable().BuildMockDbSet();
        _applicationRepo.Query().Returns(applicationsQuery);

        _permissionCheckerService.IsApplicationOwner(application, dbUser.Id!.Value.ToString()).Returns(true);
        _permissionCheckerService.IsAdmin().Returns(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Single(result.Value);
        
        var contributor = result.Value.First();
        Assert.NotNull(contributor.Authorization);
        Assert.NotNull(contributor.Authorization.Permissions);
        Assert.Single(contributor.Authorization.Permissions);
        Assert.Equal(applicationId, contributor.Authorization.Permissions.First().ApplicationId);
        Assert.Equal(ResourceType.Application, contributor.Authorization.Permissions.First().ResourceType);
        Assert.Equal(AccessType.Read, contributor.Authorization.Permissions.First().AccessType);
        Assert.NotNull(contributor.Authorization.Roles);
        Assert.Single(contributor.Authorization.Roles);
        Assert.Equal("Contributor", contributor.Authorization.Roles.First());
    }
} 