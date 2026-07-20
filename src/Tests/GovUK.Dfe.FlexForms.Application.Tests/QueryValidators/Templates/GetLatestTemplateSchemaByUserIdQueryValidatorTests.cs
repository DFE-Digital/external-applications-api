using FluentValidation.TestHelper;
using GovUK.Dfe.FlexForms.Application.Templates.Queries;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;

namespace GovUK.Dfe.FlexForms.Application.Tests.QueryValidators.Templates;

public class GetLatestTemplateSchemaByUserIdQueryValidatorTests
{
    private readonly GetLatestTemplateSchemaByUserIdQueryValidator _validator;

    public GetLatestTemplateSchemaByUserIdQueryValidatorTests()
    {
        _validator = new GetLatestTemplateSchemaByUserIdQueryValidator();
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenAllPropertiesValid()
    {
        // Arrange
        var query = new GetLatestTemplateSchemaByUserIdQuery(
            Guid.NewGuid(),
            new UserId(Guid.NewGuid()));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenTemplateIdIsEmpty()
    {
        // Arrange
        var query = new GetLatestTemplateSchemaByUserIdQuery(
            Guid.Empty,
            new UserId(Guid.NewGuid()));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.TemplateId)
            .WithErrorMessage("Template ID is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenUserIdIsNull()
    {
        // Arrange
        var query = new GetLatestTemplateSchemaByUserIdQuery(
            Guid.NewGuid(),
            null!);

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }

    [Fact]
    public void Validate_ShouldFail_WhenUserIdIsEmpty()
    {
        // Arrange
        var query = new GetLatestTemplateSchemaByUserIdQuery(
            Guid.NewGuid(),
            new UserId(Guid.Empty));

        // Act
        var result = _validator.TestValidate(query);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.UserId)
            .WithErrorMessage("User ID is required");
    }
}

