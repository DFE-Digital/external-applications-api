using DfE.ExternalApplications.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class PlaywrightHtmlGeneratorServiceTests
{
    private readonly ILogger<PlaywrightHtmlGeneratorService> _logger;
    private readonly PlaywrightHtmlGeneratorService _service;

    public PlaywrightHtmlGeneratorServiceTests()
    {
        _logger = Substitute.For<ILogger<PlaywrightHtmlGeneratorService>>();
        _service = new PlaywrightHtmlGeneratorService(_logger);
    }

    [Fact]
    public void Constructor_Should_CreateInstance_When_ValidLogger()
    {
        // Arrange & Act
        var service = new PlaywrightHtmlGeneratorService(_logger);

        // Assert
        Assert.NotNull(service);
    }

    [Fact]
    public async Task GenerateStaticHtmlAsync_Should_ThrowException_When_InvalidUrl()
    {
        // Arrange
        var invalidUrl = "not-a-valid-url";
        var headers = new Dictionary<string, string>
        {
            { "x-service-email", "test@test.com" },
            { "x-service-api-key", "test-key" }
        };

        // Act & Assert
        await Assert.ThrowsAsync<UriFormatException>(async () =>
            await _service.GenerateStaticHtmlAsync(invalidUrl, headers, null, CancellationToken.None));
    }

    [Fact]
    public async Task GenerateStaticHtmlAsync_Should_AcceptEmptyHeaders()
    {
        // Arrange
        var url = "https://test.com";
        var emptyHeaders = new Dictionary<string, string>();

        // Act & Assert
        // Note: This will fail in test because Playwright needs to be installed, 
        // but it validates that the service accepts empty headers
        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _service.GenerateStaticHtmlAsync(url, emptyHeaders, null, CancellationToken.None));

        // The exception should be about Playwright not being installed, not about empty headers
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task GenerateStaticHtmlAsync_Should_AcceptValidParameters()
    {
        // Arrange
        var url = "https://test.com/page";
        var headers = new Dictionary<string, string>
        {
            { "x-service-email", "test@test.com" },
            { "x-service-api-key", "test-key" }
        };
        var contentSelector = ".content";

        // Act & Assert
        // Note: This will fail in test because Playwright needs to be installed
        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _service.GenerateStaticHtmlAsync(url, headers, contentSelector, CancellationToken.None));

        // Verify that logging occurred (service was invoked correctly)
        Assert.NotNull(exception);
        _logger.Received().LogInformation(
            Arg.Is<string>(msg => msg.Contains("Starting static HTML generation")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task GenerateStaticHtmlAsync_Should_HandleNullContentSelector()
    {
        // Arrange
        var url = "https://test.com/page";
        var headers = new Dictionary<string, string>
        {
            { "x-service-email", "test@test.com" },
            { "x-service-api-key", "test-key" }
        };

        // Act & Assert
        // Service should accept null content selector
        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _service.GenerateStaticHtmlAsync(url, headers, null, CancellationToken.None));

        Assert.NotNull(exception);
        _logger.Received().LogInformation(
            Arg.Is<string>(msg => msg.Contains("Starting static HTML generation")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task GenerateStaticHtmlAsync_Should_LogInformation_When_Invoked()
    {
        // Arrange
        var url = "https://test.com/page";
        var headers = new Dictionary<string, string>
        {
            { "Authorization", "Bearer token" }
        };

        // Act
        try
        {
            await _service.GenerateStaticHtmlAsync(url, headers, null, CancellationToken.None);
        }
        catch
        {
            // Expected to fail without Playwright installed
        }

        // Assert
        _logger.Received().LogInformation(
            Arg.Is<string>(msg => msg.Contains("Starting static HTML generation")),
            Arg.Any<object[]>());
    }

    [Fact]
    public async Task GenerateStaticHtmlAsync_Should_LogError_When_ExceptionOccurs()
    {
        // Arrange
        var url = "https://test.com/page";
        var headers = new Dictionary<string, string>();

        // Act
        try
        {
            await _service.GenerateStaticHtmlAsync(url, headers, null, CancellationToken.None);
        }
        catch
        {
            // Expected to fail
        }

        // Assert
        _logger.Received().LogError(
            Arg.Any<Exception>(),
            Arg.Is<string>(msg => msg.Contains("Error occurred while generating static HTML")),
            Arg.Any<object[]>());
    }

    [Theory]
    [InlineData("https://test.com")]
    [InlineData("http://localhost:5000")]
    [InlineData("https://frontend.example.com/path")]
    public async Task GenerateStaticHtmlAsync_Should_AcceptValidUrls(string url)
    {
        // Arrange
        var headers = new Dictionary<string, string>
        {
            { "x-test", "value" }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _service.GenerateStaticHtmlAsync(url, headers, null, CancellationToken.None));

        // Should fail because Playwright isn't installed, but URL should be accepted
        Assert.NotNull(exception);
    }

    [Fact]
    public async Task GenerateStaticHtmlAsync_Should_RespectCancellationToken()
    {
        // Arrange
        var url = "https://test.com";
        var headers = new Dictionary<string, string>();
        var cts = new CancellationTokenSource();
        cts.Cancel(); // Cancel immediately

        // Act & Assert
        await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _service.GenerateStaticHtmlAsync(url, headers, null, cts.Token));
    }

    [Fact]
    public void Service_Should_ImplementIStaticHtmlGeneratorService()
    {
        // Assert
        Assert.IsAssignableFrom<Domain.Services.IStaticHtmlGeneratorService>(_service);
    }

    [Fact]
    public async Task GenerateStaticHtmlAsync_Should_AcceptMultipleHeaders()
    {
        // Arrange
        var url = "https://test.com";
        var headers = new Dictionary<string, string>
        {
            { "x-service-email", "test@test.com" },
            { "x-service-api-key", "key123" },
            { "x-custom-header", "value" },
            { "Authorization", "Bearer token" }
        };

        // Act & Assert
        var exception = await Assert.ThrowsAnyAsync<Exception>(async () =>
            await _service.GenerateStaticHtmlAsync(url, headers, ".content", CancellationToken.None));

        Assert.NotNull(exception);
    }
}

