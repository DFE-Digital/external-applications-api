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
        TemplateVersionId templateVersionId,
        string initialResponseBody)
    {
        // Arrange
        var command = new CreateApplicationCommand(
            applicationReference,
            templateVersionId,
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
    public void Validate_ShouldFail_WhenApplicationReferenceEmpty(string applicationReference)
    {
        // Arrange
        var command = new CreateApplicationCommand(
            applicationReference,
            new TemplateVersionId(Guid.NewGuid()),
            "Initial response");
        var validator = new CreateApplicationCommandValidator();

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.ApplicationReference));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenInitialResponseBodyEmpty(string initialResponseBody)
    {
        // Arrange
        var command = new CreateApplicationCommand(
            "APP-001",
            new TemplateVersionId(Guid.NewGuid()),
            initialResponseBody);
        var validator = new CreateApplicationCommandValidator();

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.InitialResponseBody));
    }

    [Fact]
    public void Validate_ShouldFail_WhenTemplateVersionIdNull()
    {
        // Arrange
        var command = new CreateApplicationCommand(
            "APP-001",
            null!,
            "Initial response");
        var validator = new CreateApplicationCommandValidator();

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.TemplateVersionId));
    }
} 