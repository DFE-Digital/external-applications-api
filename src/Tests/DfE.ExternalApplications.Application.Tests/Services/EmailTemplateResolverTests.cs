using DfE.ExternalApplications.Application.Common.Models;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class EmailTemplateResolverTests
{
    private readonly ILogger<EmailTemplateResolver> _logger;
    private readonly ApplicationTemplatesConfiguration _appTemplatesConfig;
    private readonly EmailTemplatesConfiguration _emailTemplatesConfig;
    private readonly EmailTemplateResolver _resolver;

    public EmailTemplateResolverTests()
    {
        _logger = Substitute.For<ILogger<EmailTemplateResolver>>();
        
        _appTemplatesConfig = new ApplicationTemplatesConfiguration
        {
            HostMappings = new Dictionary<string, string>
            {
                ["transfer"] = "9A4E9C58-9135-468C-B154-7B966F7ACFB7",
                ["sigchange"] = "B2F8E7D4-2C46-4A91-8E73-9D5A1F4B6C89"
            }
        };

        _emailTemplatesConfig = new EmailTemplatesConfiguration
        {
            ["Transfer"] = new Dictionary<string, string>
            {
                ["ApplicationSubmitted"] = "a4188604-0053-4d77-9ad2-720f6fbbdf0a",
                ["ContributorInvited"] = "b5299715-1164-5e88-ae3f-8c077g8bdg1b"
            },
            ["SigChange"] = new Dictionary<string, string>
            {
                ["ApplicationSubmitted"] = "c6300826-2275-6f99-bf40-9d188h9ceh2c",
                ["ContributorInvited"] = "d7411937-3386-7g00-cg51-ae299i0dfj3d"
            }
        };

        var appTemplatesOptions = Options.Create(_appTemplatesConfig);
        var emailTemplatesOptions = Options.Create(_emailTemplatesConfig);

        _resolver = new EmailTemplateResolver(appTemplatesOptions, emailTemplatesOptions, _logger);
    }

    [Fact]
    public async Task ResolveEmailTemplateAsync_ShouldReturnCorrectTemplate_ForTransferApplicationSubmitted()
    {
        // Arrange
        var templateId = new TemplateId(Guid.Parse("9A4E9C58-9135-468C-B154-7B966F7ACFB7"));
        var emailType = "ApplicationSubmitted";

        // Act
        var result = await _resolver.ResolveEmailTemplateAsync(templateId, emailType);

        // Assert
        Assert.Equal("a4188604-0053-4d77-9ad2-720f6fbbdf0a", result);
    }

    [Fact]
    public async Task ResolveEmailTemplateAsync_ShouldReturnCorrectTemplate_ForSigChangeApplicationSubmitted()
    {
        // Arrange
        var templateId = new TemplateId(Guid.Parse("B2F8E7D4-2C46-4A91-8E73-9D5A1F4B6C89"));
        var emailType = "ApplicationSubmitted";

        // Act
        var result = await _resolver.ResolveEmailTemplateAsync(templateId, emailType);

        // Assert
        Assert.Equal("c6300826-2275-6f99-bf40-9d188h9ceh2c", result);
    }

    [Fact]
    public async Task ResolveEmailTemplateAsync_ShouldReturnCorrectTemplate_ForTransferContributorInvited()
    {
        // Arrange
        var templateId = new TemplateId(Guid.Parse("9A4E9C58-9135-468C-B154-7B966F7ACFB7"));
        var emailType = "ContributorInvited";

        // Act
        var result = await _resolver.ResolveEmailTemplateAsync(templateId, emailType);

        // Assert
        Assert.Equal("b5299715-1164-5e88-ae3f-8c077g8bdg1b", result);
    }

    [Fact]
    public async Task ResolveEmailTemplateAsync_ShouldReturnNull_ForUnknownTemplate()
    {
        // Arrange
        var templateId = new TemplateId(Guid.Parse("00000000-0000-0000-0000-000000000000"));
        var emailType = "ApplicationSubmitted";

        // Act
        var result = await _resolver.ResolveEmailTemplateAsync(templateId, emailType);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveEmailTemplateAsync_ShouldReturnNull_ForUnknownEmailType()
    {
        // Arrange
        var templateId = new TemplateId(Guid.Parse("9A4E9C58-9135-468C-B154-7B966F7ACFB7"));
        var emailType = "UnknownEmailType";

        // Act
        var result = await _resolver.ResolveEmailTemplateAsync(templateId, emailType);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task GetApplicationTypeAsync_ShouldReturnCorrectType_ForTransfer()
    {
        // Arrange
        var templateId = new TemplateId(Guid.Parse("9A4E9C58-9135-468C-B154-7B966F7ACFB7"));

        // Act
        var result = await _resolver.GetApplicationTypeAsync(templateId);

        // Assert
        Assert.Equal("Transfer", result);
    }

    [Fact]
    public async Task GetApplicationTypeAsync_ShouldReturnCorrectType_ForSigChange()
    {
        // Arrange
        var templateId = new TemplateId(Guid.Parse("B2F8E7D4-2C46-4A91-8E73-9D5A1F4B6C89"));

        // Act
        var result = await _resolver.GetApplicationTypeAsync(templateId);

        // Assert
        Assert.Equal("SigChange", result);
    }

    [Fact]
    public async Task GetApplicationTypeAsync_ShouldReturnNull_ForUnknownTemplate()
    {
        // Arrange
        var templateId = new TemplateId(Guid.Parse("00000000-0000-0000-0000-000000000000"));

        // Act
        var result = await _resolver.GetApplicationTypeAsync(templateId);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public async Task ResolveEmailTemplateAsync_ShouldLogWarning_WhenTemplateNotFound()
    {
        // Arrange
        var templateId = new TemplateId(Guid.Parse("00000000-0000-0000-0000-000000000000"));
        var emailType = "ApplicationSubmitted";

        // Act
        await _resolver.ResolveEmailTemplateAsync(templateId, emailType);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Template ID 00000000-0000-0000-0000-000000000000 not found in host mappings")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ResolveEmailTemplateAsync_ShouldLogWarning_WhenEmailTypeNotFound()
    {
        // Arrange
        var templateId = new TemplateId(Guid.Parse("9A4E9C58-9135-468C-B154-7B966F7ACFB7"));
        var emailType = "UnknownEmailType";

        // Act
        await _resolver.ResolveEmailTemplateAsync(templateId, emailType);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Could not find email template for application type Transfer and email type UnknownEmailType")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task ResolveEmailTemplateAsync_ShouldLogDebug_WhenTemplateResolved()
    {
        // Arrange
        var templateId = new TemplateId(Guid.Parse("9A4E9C58-9135-468C-B154-7B966F7ACFB7"));
        var emailType = "ApplicationSubmitted";

        // Act
        await _resolver.ResolveEmailTemplateAsync(templateId, emailType);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Debug,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Resolved email template a4188604-0053-4d77-9ad2-720f6fbbdf0a for application type Transfer and email type ApplicationSubmitted")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }
}
