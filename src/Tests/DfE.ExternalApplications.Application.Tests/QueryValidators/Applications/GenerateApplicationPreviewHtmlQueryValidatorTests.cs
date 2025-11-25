using DfE.ExternalApplications.Application.Applications.Queries;
using FluentValidation.TestHelper;

namespace DfE.ExternalApplications.Application.Tests.QueryValidators.Applications;

public class GenerateApplicationPreviewHtmlQueryValidatorTests
{
    private readonly GenerateApplicationPreviewHtmlQueryValidator _validator = new();

    [Theory]
    [InlineData("APP-20250101-001")]
    [InlineData("TRF-20241231-999")]
    [InlineData("A")]
    [InlineData("TEST-REF-123")]
    public void Should_Pass_When_ApplicationReference_IsValid(string applicationReference)
    {
        // Arrange
        var query = new GenerateApplicationPreviewHtmlQuery(applicationReference);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Should_Fail_When_ApplicationReference_IsEmptyOrWhitespace(string applicationReference)
    {
        // Arrange
        var query = new GenerateApplicationPreviewHtmlQuery(applicationReference);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ApplicationReference)
            .WithErrorMessage("Application reference is required");
    }

    [Fact]
    public void Should_Fail_When_ApplicationReference_IsNull()
    {
        // Arrange
        var query = new GenerateApplicationPreviewHtmlQuery(null!);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.ApplicationReference)
            .WithErrorMessage("Application reference is required");
    }

    [Fact]
    public void Should_Pass_When_Valid_ApplicationReference_Provided()
    {
        // Arrange
        var query = new GenerateApplicationPreviewHtmlQuery("TRF-20241123-001");

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}

