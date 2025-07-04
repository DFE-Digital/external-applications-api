using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.Commands;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Application.Tests.CommandValidators.Applications;

public class AddApplicationResponseCommandValidatorTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Validate_ShouldSucceed_WhenAllPropertiesValid(
        Guid applicationId,
        string responseBody)
    {
        // Arrange
        var command = new AddApplicationResponseCommand(
            applicationId,
            responseBody);
        var validator = new AddApplicationResponseCommandValidator();

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenResponseBodyEmpty(string responseBody)
    {
        // Arrange
        var command = new AddApplicationResponseCommand(
            Guid.NewGuid(),
            responseBody);
        var validator = new AddApplicationResponseCommandValidator();

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.ResponseBody));
    }

    [Fact]
    public void Validate_ShouldFail_WhenApplicationIdEmpty()
    {
        // Arrange
        var command = new AddApplicationResponseCommand(
            Guid.Empty,
            "Valid response body");
        var validator = new AddApplicationResponseCommandValidator();

        // Act
        var result = validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(command.ApplicationId));
    }
} 