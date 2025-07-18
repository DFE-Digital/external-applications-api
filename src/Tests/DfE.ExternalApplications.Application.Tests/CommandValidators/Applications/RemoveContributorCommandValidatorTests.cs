using DfE.ExternalApplications.Application.Applications.Commands;
using FluentValidation.TestHelper;
using Xunit;

namespace DfE.ExternalApplications.Application.Tests.CommandValidators.Applications;

public class RemoveContributorCommandValidatorTests
{
    private readonly RemoveContributorCommandValidator _validator;

    public RemoveContributorCommandValidatorTests()
    {
        _validator = new RemoveContributorCommandValidator();
    }

    [Fact]
    public void Should_Pass_When_Valid_Request()
    {
        // Arrange
        var command = new RemoveContributorCommand(
            Guid.NewGuid(),
            Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_ApplicationId_Is_Empty()
    {
        // Arrange
        var command = new RemoveContributorCommand(
            Guid.Empty,
            Guid.NewGuid());

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ApplicationId);
    }

    [Fact]
    public void Should_Fail_When_UserId_Is_Empty()
    {
        // Arrange
        var command = new RemoveContributorCommand(
            Guid.NewGuid(),
            Guid.Empty);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId);
    }
} 