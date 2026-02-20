using DfE.ExternalApplications.Api.Middleware;
using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

namespace DfE.ExternalApplications.Api.Tests.Middleware;

public class TenantResolutionMiddlewareTests
{
    private readonly ITenantConfigurationProvider _tenantConfigProvider;
    private readonly ILogger<TenantResolutionMiddleware> _logger;

    public TenantResolutionMiddlewareTests()
    {
        _tenantConfigProvider = Substitute.For<ITenantConfigurationProvider>();
        _logger = Substitute.For<ILogger<TenantResolutionMiddleware>>();
    }

    private TenantConfiguration CreateTenant(Guid id, string name, params string[] origins)
    {
        return new TenantConfiguration(id, name, new ConfigurationBuilder().Build(), origins);
    }

    private DefaultHttpContext CreateHttpContext()
    {
        var context = new DefaultHttpContext();
        var services = new ServiceCollection();
        var tenantAccessor = new TestTenantContextAccessor();
        services.AddScoped<ITenantContextAccessor>(_ => tenantAccessor);
        context.RequestServices = services.BuildServiceProvider();
        return context;
    }

    [Fact]
    public async Task InvokeAsync_ShouldResolveTenant_FromHeader()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId, "TestTenant");
        _tenantConfigProvider.GetTenant(tenantId).Returns(tenant);

        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            _tenantConfigProvider, _logger);

        var context = CreateHttpContext();
        context.Request.Headers[TenantResolutionMiddleware.TenantIdHeader] = tenantId.ToString();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        var accessor = context.RequestServices.GetRequiredService<ITenantContextAccessor>();
        Assert.Equal(tenant, accessor.CurrentTenant);
    }

    [Fact]
    public async Task InvokeAsync_ShouldResolveTenant_FromOriginHeader()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        var tenant = CreateTenant(tenantId, "TestTenant", "https://app.example.com");
        _tenantConfigProvider.GetAllTenants().Returns(new[] { tenant });

        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            _tenantConfigProvider, _logger);

        var context = CreateHttpContext();
        context.Request.Headers["Origin"] = "https://app.example.com";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
        var accessor = context.RequestServices.GetRequiredService<ITenantContextAccessor>();
        Assert.Equal(tenant, accessor.CurrentTenant);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn400_WhenTenantHeaderInvalid()
    {
        // Arrange
        var tenantId = Guid.NewGuid();
        _tenantConfigProvider.GetTenant(tenantId).Returns((TenantConfiguration?)null);

        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            _tenantConfigProvider, _logger);

        var context = CreateHttpContext();
        context.Request.Headers[TenantResolutionMiddleware.TenantIdHeader] = tenantId.ToString();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(400, context.Response.StatusCode);
    }

    [Fact]
    public async Task InvokeAsync_ShouldReturn400_WhenNoTenantHeader_AndNoOriginMatch()
    {
        // Arrange
        _tenantConfigProvider.GetAllTenants().Returns(Array.Empty<TenantConfiguration>());

        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            _tenantConfigProvider, _logger);

        var context = CreateHttpContext();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.False(nextCalled);
        Assert.Equal(400, context.Response.StatusCode);
    }

    [Theory]
    [InlineData("/swagger")]
    [InlineData("/swagger/index.html")]
    [InlineData("/health")]
    [InlineData("/_something")]
    public async Task InvokeAsync_ShouldBypassTenantResolution_ForInfrastructurePaths(string path)
    {
        // Arrange
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            _tenantConfigProvider, _logger);

        var context = CreateHttpContext();
        context.Request.Path = path;

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    [Fact]
    public async Task InvokeAsync_ShouldBypassTenantResolution_ForOptionsMethod()
    {
        // Arrange
        var nextCalled = false;
        var middleware = new TenantResolutionMiddleware(
            _ => { nextCalled = true; return Task.CompletedTask; },
            _tenantConfigProvider, _logger);

        var context = CreateHttpContext();
        context.Request.Method = "OPTIONS";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(nextCalled);
    }

    private class TestTenantContextAccessor : ITenantContextAccessor
    {
        public TenantConfiguration? CurrentTenant { get; set; }
    }
}
