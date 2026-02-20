using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class InternalAuthRequestCheckerTests
{
    private readonly ITenantContextAccessor _tenantContextAccessor;
    private readonly ILogger<InternalAuthRequestChecker> _logger;
    private readonly InternalAuthRequestChecker _checker;

    public InternalAuthRequestCheckerTests()
    {
        _tenantContextAccessor = Substitute.For<ITenantContextAccessor>();
        _logger = Substitute.For<ILogger<InternalAuthRequestChecker>>();
        _checker = new InternalAuthRequestChecker(_tenantContextAccessor, _logger);
    }

    private TenantConfiguration CreateTenantWithAuth(string email, string apiKey)
    {
        var settings = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["InternalServiceAuth:Services:0:Email"] = email,
                ["InternalServiceAuth:Services:0:ApiKey"] = apiKey
            })
            .Build();

        return new TenantConfiguration(Guid.NewGuid(), "TestTenant", settings, Array.Empty<string>());
    }

    [Fact]
    public void IsValidRequest_ShouldReturnTrue_WhenHeadersMatchConfiguration()
    {
        // Arrange
        var tenant = CreateTenantWithAuth("service@example.com", "secret-key-123");
        _tenantContextAccessor.CurrentTenant.Returns(tenant);

        var context = new DefaultHttpContext();
        context.Request.Headers["x-service-email"] = "service@example.com";
        context.Request.Headers["x-service-api-key"] = "secret-key-123";

        // Act
        var result = _checker.IsValidRequest(context);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidRequest_ShouldReturnFalse_WhenApiKeyDoesNotMatch()
    {
        // Arrange
        var tenant = CreateTenantWithAuth("service@example.com", "secret-key-123");
        _tenantContextAccessor.CurrentTenant.Returns(tenant);

        var context = new DefaultHttpContext();
        context.Request.Headers["x-service-email"] = "service@example.com";
        context.Request.Headers["x-service-api-key"] = "wrong-key";

        // Act
        var result = _checker.IsValidRequest(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidRequest_ShouldReturnFalse_WhenEmailNotConfigured()
    {
        // Arrange
        var tenant = CreateTenantWithAuth("service@example.com", "secret-key-123");
        _tenantContextAccessor.CurrentTenant.Returns(tenant);

        var context = new DefaultHttpContext();
        context.Request.Headers["x-service-email"] = "unknown@example.com";
        context.Request.Headers["x-service-api-key"] = "secret-key-123";

        // Act
        var result = _checker.IsValidRequest(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidRequest_ShouldReturnFalse_WhenNoTenantContext()
    {
        // Arrange
        _tenantContextAccessor.CurrentTenant.Returns((TenantConfiguration?)null);

        var context = new DefaultHttpContext();
        context.Request.Headers["x-service-email"] = "service@example.com";
        context.Request.Headers["x-service-api-key"] = "secret-key-123";

        // Act
        var result = _checker.IsValidRequest(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidRequest_ShouldReturnFalse_WhenNoServicesConfigured()
    {
        // Arrange
        var settings = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();
        var tenant = new TenantConfiguration(Guid.NewGuid(), "TestTenant", settings, Array.Empty<string>());
        _tenantContextAccessor.CurrentTenant.Returns(tenant);

        var context = new DefaultHttpContext();
        context.Request.Headers["x-service-email"] = "service@example.com";
        context.Request.Headers["x-service-api-key"] = "secret-key-123";

        // Act
        var result = _checker.IsValidRequest(context);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidRequest_ShouldBeCaseInsensitive_ForEmail()
    {
        // Arrange
        var tenant = CreateTenantWithAuth("Service@Example.COM", "secret-key-123");
        _tenantContextAccessor.CurrentTenant.Returns(tenant);

        var context = new DefaultHttpContext();
        context.Request.Headers["x-service-email"] = "service@example.com";
        context.Request.Headers["x-service-api-key"] = "secret-key-123";

        // Act
        var result = _checker.IsValidRequest(context);

        // Assert
        Assert.True(result);
    }
}
