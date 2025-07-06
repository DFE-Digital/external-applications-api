using DfE.ExternalApplications.Application.Applications.Queries;

namespace DfE.ExternalApplications.Application.Tests.QueryValidators.Applications;

public class GetApplicationByReferenceQueryValidatorTests
{
    [Theory]
    [InlineData("APP-20250101-001")]
    [InlineData("APP-20241231-999")]
    [InlineData("A")]
    [InlineData("12345678901234567890")] // 20 characters
    public void Validate_ShouldSucceed_WhenApplicationReferenceValid(string applicationReference)
    {
        // Arrange
        var query = new GetApplicationByReferenceQuery(applicationReference);
        var validator = new GetApplicationByReferenceQueryValidator();

        // Act
        var result = validator.Validate(query);

        // Assert
        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenApplicationReferenceEmpty(string applicationReference)
    {
        // Arrange
        var query = new GetApplicationByReferenceQuery(applicationReference);
        var validator = new GetApplicationByReferenceQueryValidator();

        // Act
        var result = validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(query.ApplicationReference));
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("required"));
    }

    [Fact]
    public void Validate_ShouldFail_WhenApplicationReferenceTooLong()
    {
        // Arrange
        var longReference = new string('A', 21); // 21 characters
        var query = new GetApplicationByReferenceQuery(longReference);
        var validator = new GetApplicationByReferenceQueryValidator();

        // Act
        var result = validator.Validate(query);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == nameof(query.ApplicationReference));
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("cannot exceed 20 characters"));
    }
} 