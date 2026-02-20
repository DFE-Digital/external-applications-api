using DfE.ExternalApplications.Application.Templates.Commands;
using FluentValidation.TestHelper;

namespace DfE.ExternalApplications.Application.Tests.CommandValidators.Templates;

public class CreateTemplateVersionCommandValidatorTests
{
    private readonly CreateTemplateVersionCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenAllPropertiesValid()
    {
        var jsonSchema = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"type\":\"object\"}"));
        var command = new CreateTemplateVersionCommand(Guid.NewGuid(), "1.0.0", jsonSchema);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_ShouldFail_WhenTemplateIdIsEmpty()
    {
        var jsonSchema = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"type\":\"object\"}"));
        var command = new CreateTemplateVersionCommand(Guid.Empty, "1.0.0", jsonSchema);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.TemplateId);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenVersionNumberIsEmpty(string? versionNumber)
    {
        var jsonSchema = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"type\":\"object\"}"));
        var command = new CreateTemplateVersionCommand(Guid.NewGuid(), versionNumber!, jsonSchema);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.VersionNumber);
    }

    [Theory]
    [InlineData("1.0")]
    [InlineData("1")]
    [InlineData("abc")]
    [InlineData("1.0.0.0")]
    [InlineData("v1.0.0")]
    public void Validate_ShouldFail_WhenVersionNumberFormatIsInvalid(string versionNumber)
    {
        var jsonSchema = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"type\":\"object\"}"));
        var command = new CreateTemplateVersionCommand(Guid.NewGuid(), versionNumber, jsonSchema);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.VersionNumber);
    }

    [Theory]
    [InlineData("0.0.1")]
    [InlineData("1.0.0")]
    [InlineData("10.20.30")]
    public void Validate_ShouldSucceed_WhenVersionNumberFormatIsValid(string versionNumber)
    {
        var jsonSchema = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"type\":\"object\"}"));
        var command = new CreateTemplateVersionCommand(Guid.NewGuid(), versionNumber, jsonSchema);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.VersionNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenJsonSchemaIsEmpty(string? jsonSchema)
    {
        var command = new CreateTemplateVersionCommand(Guid.NewGuid(), "1.0.0", jsonSchema!);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.JsonSchema);
    }

    [Fact]
    public void Validate_ShouldFail_WhenJsonSchemaIsNotValidBase64()
    {
        var command = new CreateTemplateVersionCommand(Guid.NewGuid(), "1.0.0", "not-valid-base64!!!");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.JsonSchema);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenJsonSchemaIsValidBase64()
    {
        var jsonSchema = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("{\"type\":\"object\"}"));
        var command = new CreateTemplateVersionCommand(Guid.NewGuid(), "1.0.0", jsonSchema);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.JsonSchema);
    }
}
