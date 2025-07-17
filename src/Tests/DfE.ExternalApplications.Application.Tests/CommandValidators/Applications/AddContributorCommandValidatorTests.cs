using DfE.ExternalApplications.Application.Applications.Commands;
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
    public void Validate_WithValidCommand_ShouldPass()
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            "John Doe",
            "test@example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyApplicationId_ShouldFail()
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.Empty,
            "John Doe",
            "test@example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddContributorCommand.ApplicationId));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Validate_WithInvalidName_ShouldFail(string name)
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            name,
            "test@example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddContributorCommand.Name));
    }

    [Fact]
    public void Validate_WithNameExceedingMaximumLength_ShouldFail()
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            new string('A', 101), // 101 characters
            "test@example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddContributorCommand.Name));
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Validate_WithInvalidEmail_ShouldFail(string email)
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            "John Doe",
            email);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddContributorCommand.Email));
    }

    [Theory]
    [InlineData("invalid-email")]
    [InlineData("test@")]
    [InlineData("@example.com")]
    [InlineData("test.example.com")]
    public void Validate_WithInvalidEmailFormat_ShouldFail(string email)
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            "John Doe",
            email);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddContributorCommand.Email));
    }

    [Fact]
    public void Validate_WithEmailExceedingMaximumLength_ShouldFail()
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            "John Doe",
            new string('A', 250) + "@example.com"); // 257 characters

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddContributorCommand.Email));
    }

    [Fact]
    public void Validate_WithMultipleValidationErrors_ShouldReturnAllErrors()
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.Empty,
            "",
            "");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(3, result.Errors.Count);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddContributorCommand.ApplicationId));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddContributorCommand.Name));
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(AddContributorCommand.Email));
    }

    [Theory]
    [InlineData("test@example.com")]
    [InlineData("user.name@domain.co.uk")]
    [InlineData("user+tag@example.org")]
    [InlineData("user123@test-domain.com")]
    public void Validate_WithValidEmailFormats_ShouldPass(string email)
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            "John Doe",
            email);

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("John Doe")]
    [InlineData("Mary-Jane Smith")]
    [InlineData("O'Connor")]
    [InlineData("José García")]
    public void Validate_WithValidNames_ShouldPass(string name)
    {
        // Arrange
        var command = new AddContributorCommand(
            Guid.NewGuid(),
            name,
            "test@example.com");

        // Act
        var result = _validator.Validate(command);

        // Assert
        Assert.True(result.IsValid);
    }
} 