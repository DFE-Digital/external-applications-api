using DfE.ExternalApplications.Application.Applications.Commands;
using FluentValidation.TestHelper;
using Xunit;

namespace DfE.ExternalApplications.Application.Tests.CommandValidators.Applications;

public class AddContributorCommandValidatorTests
{
    private readonly AddContributorCommandValidator _validator;

    public AddContributorCommandValidatorTests()
    {
        _validator = new AddContributorCommandValidator();
    }

    [Fact]
    public void Should_Pass_When_Valid_Request()
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            "John Doe",
            "john.doe@example.com");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Fail_When_ApplicationId_Is_Empty()
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.Empty,
            "John Doe",
            "john.doe@example.com");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ApplicationId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Should_Fail_When_Name_Is_Empty(string name)
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            name,
            "john.doe@example.com");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Should_Fail_When_Name_Exceeds_Maximum_Length()
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            new string('A', 101), // 101 characters
            "john.doe@example.com");

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Should_Fail_When_Email_Is_Empty(string email)
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            "John Doe",
            email);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    [InlineData("test.example.com")]
    public void Should_Fail_When_Email_Is_Invalid(string email)
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            "John Doe",
            email);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Fact]
    public void Should_Fail_When_Email_Exceeds_Maximum_Length()
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            "John Doe",
            new string('A', 250) + "@example.com"); // 257 characters

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Email);
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("user+tag@example.org")]
    public void Should_Pass_When_Email_Is_Valid(string email)
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            "John Doe",
            email);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Email);
    }
} 