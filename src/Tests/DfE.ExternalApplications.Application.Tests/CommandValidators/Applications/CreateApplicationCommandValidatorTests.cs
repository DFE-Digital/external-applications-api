using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Application.Tests.CommandValidators.Applications;

public class CreateApplicationCommandValidatorTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Validate_ShouldSucceed_WhenAllPropertiesValid(
        string applicationReference,
        TemplateId templateId,
        string initialResponseBody)
    {
        // Arrange
        var command = new CreateApplicationCommand(
            templateId.Value,
            initialResponseBody);
        var validator = new CreateApplicationCommandValidator();

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenInitialResponseBodyEmpty(string responseBody)
    {
        // Arrange
        var command = new CreateApplicationCommand(
            Guid.NewGuid(),
            responseBody);
        var validator = new CreateApplicationCommandValidator();

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.InitialResponseBody));
    }

    [Fact]
    public void Validate_ShouldFail_WhenTemplateIdEmpty()
    {
        // Arrange
        var command = new CreateApplicationCommand(
            Guid.Empty,
            "Initial response");
        var validator = new CreateApplicationCommandValidator();

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.TemplateId));
    }
} 