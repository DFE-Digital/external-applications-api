using System.Security.Claims;
using AutoFixture.Xunit2;
using GovUK.Dfe.CoreLibs.Caching.Interfaces;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Api.Security;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.Tenancy;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.JsonWebTokens;
using NSubstitute;
using Xunit;
using MockQueryable.NSubstitute;

namespace DfE.ExternalApplications.Api.Tests.Security.ClaimProviders;

public class UserPermissionClaimProviderTests
{
    private readonly ISender _sender;
    private readonly ILogger<UserPermissionClaimProvider> _logger;
    private readonly IEaRepository<User> _userRepo;
    private readonly ICacheService<IRedisCacheType> _cacheService;
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly UserPermissionClaimProvider _provider;

    public UserPermissionClaimProviderTests()
    {
        _sender = Substitute.For<ISender>();
        _logger = Substitute.For<ILogger<UserPermissionClaimProvider>>();
        _userRepo = Substitute.For<IEaRepository<User>>();
        _cacheService = Substitute.For<ICacheService<IRedisCacheType>>();
        _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
        _provider = new UserPermissionClaimProvider(_sender, _logger, _userRepo, _cacheService, _tenantContextAccessor);
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenIssuerIsWindowsNet()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://sts.windows.net/abc"),
            new Claim(ClaimTypes.Email, "test@example.com")
        }));

        // Act
        var result = await _provider.GetClaimsAsync(principal);

        // Assert
        Assert.Empty(result);
        await _cacheService.DidNotReceive().GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Any<string>(), Arg.Any<Func<Task<IEnumerable<Claim>>>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenIssuerContainsWindowsNet()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim("iss", "https://login.windows.net/tenant"),
            new Claim(ClaimTypes.Email, "test@example.com")
        }));

        // Act
        var result = await _provider.GetClaimsAsync(principal);

        // Assert
        Assert.Empty(result);
        await _cacheService.DidNotReceive().GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Any<string>(), Arg.Any<Func<Task<IEnumerable<Claim>>>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenIssuerIsNull()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Email, "test@example.com")
        }));

        // Act
        var result = await _provider.GetClaimsAsync(principal);

        // Assert
        Assert.Empty(result);
        await _cacheService.DidNotReceive().GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Any<string>(), Arg.Any<Func<Task<IEnumerable<Claim>>>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenEmailClaimMissing()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://example.com")
        }));

        // Act
        var result = await _provider.GetClaimsAsync(principal);

        // Assert
        Assert.Empty(result);
        _logger.Received(1).LogWarning("UserPermissionsClaimProvider() > User Email Address not found.");
        await _cacheService.DidNotReceive().GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Any<string>(), Arg.Any<Func<Task<IEnumerable<Claim>>>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenEmailClaimIsEmpty()
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://example.com"),
            new Claim(ClaimTypes.Email, "")
        }));

        // Act
        var result = await _provider.GetClaimsAsync(principal);

        // Assert
        Assert.Empty(result);
        _logger.Received(1).LogWarning("UserPermissionsClaimProvider() > User Email Address not found.");
        await _cacheService.DidNotReceive().GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Any<string>(), Arg.Any<Func<Task<IEnumerable<Claim>>>>(), Arg.Any<string>());
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldUseCachedClaims_WhenCacheHit()
    {
        // Arrange
        var email = "test@example.com";
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://example.com"),
            new Claim(ClaimTypes.Email, email)
        }));

        var cachedClaims = new List<Claim>
        {
            new("permission", "Application:123:Read"),
            new("permission", "Template:456:Write")
        };

        _cacheService.GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Any<string>(),
            Arg.Any<Func<Task<IEnumerable<Claim>>>>(),
            Arg.Any<string>())
            .Returns(cachedClaims);

        // Act
        var result = await _provider.GetClaimsAsync(principal);

        // Assert
        Assert.Equal(cachedClaims, result);
        await _cacheService.Received(1).GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Is<string>(key => key.StartsWith("UserClaims_")),
            Arg.Any<Func<Task<IEnumerable<Claim>>>>(),
            nameof(UserPermissionClaimProvider));
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenUserNotFound()
    {
        // Arrange
        var email = "test@example.com";
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://example.com"),
            new Claim(ClaimTypes.Email, email)
        }));

        var emptyUsers = new User[0].AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(emptyUsers);

        _cacheService.GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Any<string>(),
            Arg.Any<Func<Task<IEnumerable<Claim>>>>(),
            Arg.Any<string>())
            .Returns(callInfo => callInfo.Arg<Func<Task<IEnumerable<Claim>>>>()());

        // Act
        var result = await _provider.GetClaimsAsync(principal);

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldReturnEmpty_WhenUserHasNoRole()
    {
        // Arrange
        var email = "test@example.com";
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://example.com"),
            new Claim(ClaimTypes.Email, email)
        }));

        var userId = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        var user = new User(
            id: userId,
            roleId: roleId,
            name: "Test User",
            email: email,
            createdOn: DateTime.UtcNow,
            createdBy: null,
            lastModifiedOn: null,
            lastModifiedBy: null,
            externalProviderId: null
        );
        // Don't set the Role property - it will be null

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(users);

        _cacheService.GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Any<string>(),
            Arg.Any<Func<Task<IEnumerable<Claim>>>>(),
            Arg.Any<string>())
            .Returns(callInfo => callInfo.Arg<Func<Task<IEnumerable<Claim>>>>()());

        // Act
        var result = await _provider.GetClaimsAsync(principal);

        // Assert
        Assert.Empty(result);
    }

    [Theory, AutoData]
    public async Task GetClaimsAsync_ShouldReturnUserPermissionClaims_WhenUserHasPermissions(
        string email,
        string resourceKey)
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://example.com"),
            new Claim(ClaimTypes.Email, email)
        }));

        var userId = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        
        // Set up permissions
        var permission = new Permission(
            new PermissionId(Guid.NewGuid()),
            userId,
            null, // applicationId
            resourceKey,
            ResourceType.Application,
            AccessType.Read,
            DateTime.UtcNow,
            userId // grantedBy
        );
        var permissions = new[] { permission };
        
        var user = new User(
            id: userId,
            roleId: roleId,
            name: "Test User",
            email: email,
            createdOn: DateTime.UtcNow,
            createdBy: null,
            lastModifiedOn: null,
            lastModifiedBy: null,
            externalProviderId: null,
            initialPermissions: permissions
        );

        // Set up the role
        var role = new Role(roleId, "TestRole");
        user.GetType().GetProperty("Role")!.SetValue(user, role);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(users);

        _cacheService.GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Any<string>(),
            Arg.Any<Func<Task<IEnumerable<Claim>>>>(),
            Arg.Any<string>())
            .Returns(callInfo => callInfo.Arg<Func<Task<IEnumerable<Claim>>>>()());

        // Act
        var result = (await _provider.GetClaimsAsync(principal)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("permission", result[0].Type);
        Assert.Equal($"Application:{resourceKey}:Read", result[0].Value);
    }

    [Theory, AutoData]
    public async Task GetClaimsAsync_ShouldReturnTemplatePermissionClaims_WhenUserHasTemplatePermissions(
        string email,
        Guid templateId)
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://example.com"),
            new Claim(ClaimTypes.Email, email)
        }));

        var userId = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        
        // Set up template permissions
        var templatePermission = new TemplatePermission(
            new TemplatePermissionId(Guid.NewGuid()),
            userId,
            new TemplateId(templateId),
            AccessType.Write,
            DateTime.UtcNow,
            userId // grantedBy
        );
        var templatePermissions = new[] { templatePermission };
        
        var user = new User(
            id: userId,
            roleId: roleId,
            name: "Test User",
            email: email,
            createdOn: DateTime.UtcNow,
            createdBy: null,
            lastModifiedOn: null,
            lastModifiedBy: null,
            externalProviderId: null,
            initialTemplatePermissions: templatePermissions
        );

        // Set up the role
        var role = new Role(roleId, "TestRole");
        user.GetType().GetProperty("Role")!.SetValue(user, role);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(users);

        _cacheService.GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Any<string>(),
            Arg.Any<Func<Task<IEnumerable<Claim>>>>(),
            Arg.Any<string>())
            .Returns(callInfo => callInfo.Arg<Func<Task<IEnumerable<Claim>>>>()());

        // Act
        var result = (await _provider.GetClaimsAsync(principal)).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal("permission", result[0].Type);
        Assert.Equal($"Template:{templateId}:Write", result[0].Value);
    }

    [Theory, AutoData]
    public async Task GetClaimsAsync_ShouldReturnBothUserAndTemplatePermissions_WhenUserHasBoth(
        string email,
        string resourceKey,
        Guid templateId)
    {
        // Arrange
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://example.com"),
            new Claim(ClaimTypes.Email, email)
        }));

        var userId = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        
        // Set up user permissions
        var permission = new Permission(
            new PermissionId(Guid.NewGuid()),
            userId,
            null, // applicationId
            resourceKey,
            ResourceType.Application,
            AccessType.Read,
            DateTime.UtcNow,
            userId // grantedBy
        );
        var permissions = new[] { permission };

        // Set up template permissions
        var templatePermission = new TemplatePermission(
            new TemplatePermissionId(Guid.NewGuid()),
            userId,
            new TemplateId(templateId),
            AccessType.Write,
            DateTime.UtcNow,
            userId // grantedBy
        );
        var templatePermissions = new[] { templatePermission };
        
        var user = new User(
            id: userId,
            roleId: roleId,
            name: "Test User",
            email: email,
            createdOn: DateTime.UtcNow,
            createdBy: null,
            lastModifiedOn: null,
            lastModifiedBy: null,
            externalProviderId: null,
            initialPermissions: permissions,
            initialTemplatePermissions: templatePermissions
        );

        // Set up the role
        var role = new Role(roleId, "TestRole");
        user.GetType().GetProperty("Role")!.SetValue(user, role);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(users);

        _cacheService.GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Any<string>(),
            Arg.Any<Func<Task<IEnumerable<Claim>>>>(),
            Arg.Any<string>())
            .Returns(callInfo => callInfo.Arg<Func<Task<IEnumerable<Claim>>>>()());

        // Act
        var result = (await _provider.GetClaimsAsync(principal)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Type == "permission" && c.Value == $"Application:{resourceKey}:Read");
        Assert.Contains(result, c => c.Type == "permission" && c.Value == $"Template:{templateId}:Write");
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldHandleMultiplePermissionsOfSameType()
    {
        // Arrange
        var email = "test@example.com";
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://example.com"),
            new Claim(ClaimTypes.Email, email)
        }));

        var userId = new UserId(Guid.NewGuid());
        var roleId = new RoleId(Guid.NewGuid());
        
        // Set up multiple user permissions
        var permission1 = new Permission(
            new PermissionId(Guid.NewGuid()),
            userId,
            null, // applicationId
            "app1",
            ResourceType.Application,
            AccessType.Read,
            DateTime.UtcNow,
            userId // grantedBy
        );
        var permission2 = new Permission(
            new PermissionId(Guid.NewGuid()),
            userId,
            null, // applicationId
            "app2",
            ResourceType.Application,
            AccessType.Write,
            DateTime.UtcNow,
            userId // grantedBy
        );
        var permissions = new[] { permission1, permission2 };
        
        var user = new User(
            id: userId,
            roleId: roleId,
            name: "Test User",
            email: email,
            createdOn: DateTime.UtcNow,
            createdBy: null,
            lastModifiedOn: null,
            lastModifiedBy: null,
            externalProviderId: null,
            initialPermissions: permissions
        );

        // Set up the role
        var role = new Role(roleId, "TestRole");
        user.GetType().GetProperty("Role")!.SetValue(user, role);

        var users = new[] { user }.AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(users);

        _cacheService.GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Any<string>(),
            Arg.Any<Func<Task<IEnumerable<Claim>>>>(),
            Arg.Any<string>())
            .Returns(callInfo => callInfo.Arg<Func<Task<IEnumerable<Claim>>>>()());

        // Act
        var result = (await _provider.GetClaimsAsync(principal)).ToList();

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains(result, c => c.Type == "permission" && c.Value == "Application:app1:Read");
        Assert.Contains(result, c => c.Type == "permission" && c.Value == "Application:app2:Write");
    }

    [Fact]
    public async Task GetClaimsAsync_ShouldGenerateCorrectCacheKey()
    {
        // Arrange
        var email = "test@example.com";
        var principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(JwtRegisteredClaimNames.Iss, "https://example.com"),
            new Claim(ClaimTypes.Email, email)
        }));

        var emptyUsers = new User[0].AsQueryable().BuildMockDbSet();
        _userRepo.Query().Returns(emptyUsers);

        _cacheService.GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Any<string>(),
            Arg.Any<Func<Task<IEnumerable<Claim>>>>(),
            Arg.Any<string>())
            .Returns(callInfo => callInfo.Arg<Func<Task<IEnumerable<Claim>>>>()());

        // Act
        await _provider.GetClaimsAsync(principal);

        // Assert
        await _cacheService.Received(1).GetOrAddAsync<IEnumerable<Claim>>(
            Arg.Is<string>(key => key.StartsWith("UserClaims_") && key.Length > "UserClaims_".Length),
            Arg.Any<Func<Task<IEnumerable<Claim>>>>(),
            nameof(UserPermissionClaimProvider));
    }
}