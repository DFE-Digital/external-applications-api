using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Application.Services;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using System.Security.Claims;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class ClaimBasedPermissionCheckerServiceTests
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly ClaimBasedPermissionCheckerService _service;
    private readonly HttpContext _httpContext;
    private readonly ClaimsPrincipal _user;

    public ClaimBasedPermissionCheckerServiceTests()
    {
        _httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        _httpContext = Substitute.For<HttpContext>();
        _user = new ClaimsPrincipal(new ClaimsIdentity());
        _httpContext.User.Returns(_user);
        _httpContextAccessor.HttpContext.Returns(_httpContext);
        _service = new ClaimBasedPermissionCheckerService(_httpContextAccessor);
    }

    [Fact]
    public void HasPermission_WhenUserHasMatchingClaim_ReturnsTrue()
    {
        // Arrange
        var resourceType = ResourceType.Application;
        var resourceId = "123";
        var accessType = AccessType.Write;
        var claim = new Claim("permission", $"{resourceType}:{resourceId}:{accessType}");
        _user.AddIdentity(new ClaimsIdentity(new[] { claim }));

        // Act
        var result = _service.HasPermission(resourceType, resourceId, accessType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasPermission_WhenUserDoesNotHaveMatchingClaim_ReturnsFalse()
    {
        // Arrange
        var resourceType = ResourceType.User;
        var resourceId = "123";
        var accessType = AccessType.Write;
        var claim = new Claim("permission", $"{resourceType}:different-id:{accessType}");
        _user.AddIdentity(new ClaimsIdentity(new[] { claim }));

        // Act
        var result = _service.HasPermission(resourceType, resourceId, accessType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void HasAnyPermission_WhenUserHasMatchingClaim_ReturnsTrue()
    {
        // Arrange
        var resourceType = ResourceType.Application;
        var accessType = AccessType.Write;
        var claim = new Claim("permission", $"{resourceType}:any-id:{accessType}");
        _user.AddIdentity(new ClaimsIdentity(new[] { claim }));

        // Act
        var result = _service.HasAnyPermission(resourceType, accessType);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void HasAnyPermission_WhenUserDoesNotHaveMatchingClaim_ReturnsFalse()
    {
        // Arrange
        var resourceType = ResourceType.User;
        var accessType = AccessType.Write;
        var claim = new Claim("permission", $"DifferentType:any-id:{accessType}");
        _user.AddIdentity(new ClaimsIdentity(new[] { claim }));

        // Act
        var result = _service.HasAnyPermission(resourceType, accessType);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void GetResourceIdsWithPermission_ReturnsCorrectIds()
    {
        // Arrange
        var resourceType = ResourceType.Application;
        var accessType = AccessType.Write;
        var claims = new[]
        {
            new Claim("permission", $"{resourceType}:123:{accessType}"),
            new Claim("permission", $"{resourceType}:456:{accessType}"),
            new Claim("permission", $"{resourceType}:789:Read"),
            new Claim("permission", "DifferentType:123:{accessType}")
        };
        _user.AddIdentity(new ClaimsIdentity(claims));

        // Act
        var result = _service.GetResourceIdsWithPermission(resourceType, accessType);

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("123", result);
        Assert.Contains("456", result);
    }

    [Fact]
    public void Constructor_WhenHttpContextAccessorIsNull_ThrowsArgumentNullException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => new ClaimBasedPermissionCheckerService(null!));
    }
} 