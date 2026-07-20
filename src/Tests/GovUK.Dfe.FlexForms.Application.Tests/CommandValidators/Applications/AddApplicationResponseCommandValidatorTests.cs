using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Application.Applications.Commands;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;
using FluentValidation.TestHelper;

namespace GovUK.Dfe.FlexForms.Application.Tests.CommandValidators.Applications;

public class AddApplicationResponseCommandValidatorTests
{
    private readonly AddApplicationResponseCommandValidator _validator = new();
    
    [Fact]
    public void Validate_ShouldSucceed_WhenAllPropertiesValid()
    {
        // Arrange
        var encodedBody = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("some body"));
        var command = new AddApplicationResponseCommand(Guid.NewGuid(), encodedBody);
        
        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenResponseBodyEmpty(string responseBody)
    {
        // Arrange
        var command = new AddApplicationResponseCommand(Guid.NewGuid(), responseBody);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.ResponseBody);
    }
    
    [Fact]
    public void Validate_ShouldFail_WhenApplicationIdEmpty()
    {
        // Arrange
        var encodedBody = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("some body"));
        var command = new AddApplicationResponseCommand(Guid.Empty, encodedBody);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(c => c.ApplicationId);
    }
} 