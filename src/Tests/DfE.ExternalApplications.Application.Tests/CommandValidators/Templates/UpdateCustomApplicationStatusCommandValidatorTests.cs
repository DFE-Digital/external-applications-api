using DfE.ExternalApplications.Application.Templates.Commands;
using FluentValidation.TestHelper;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.ExternalApplications.Application.Tests.CommandValidators.Templates;

public class UpdateCustomApplicationStatusCommandValidatorTests
{
    private readonly UpdateCustomApplicationStatusCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenAllPropertiesValid()
    {
        var command = new UpdateCustomApplicationStatusCommand(
            Guid.NewGuid(),
            ApplicationStatus.Submitted,
            "Custom Label");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenLabelIsEmpty(string? label)
    {
        var command = new UpdateCustomApplicationStatusCommand(
            Guid.NewGuid(),
            ApplicationStatus.Submitted,
            label!);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Label);
    }

    [Fact]
    public void Validate_ShouldFail_WhenApplicationStatusIsInvalidEnum()
    {
        var command = new UpdateCustomApplicationStatusCommand(
            Guid.NewGuid(),
            (ApplicationStatus)999,
            "Custom Label");

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ApplicationStatus);
    }

    [Theory]
    [InlineData(ApplicationStatus.InProgress)]
    [InlineData(ApplicationStatus.Submitted)]
    public void Validate_ShouldSucceed_WhenApplicationStatusIsValid(ApplicationStatus applicationStatus)
    {
        var command = new UpdateCustomApplicationStatusCommand(
            Guid.NewGuid(),
            applicationStatus,
            "Custom Label");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.ApplicationStatus);
    }
}
