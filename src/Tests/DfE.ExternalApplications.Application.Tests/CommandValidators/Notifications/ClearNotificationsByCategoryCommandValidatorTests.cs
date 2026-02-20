using DfE.ExternalApplications.Application.Notifications.Commands;
using FluentValidation.TestHelper;

namespace DfE.ExternalApplications.Application.Tests.CommandValidators.Notifications;

public class ClearNotificationsByCategoryCommandValidatorTests
{
    private readonly ClearNotificationsByCategoryCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenCategoryIsValid()
    {
        var command = new ClearNotificationsByCategoryCommand("TestCategory");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenCategoryIsEmpty(string? category)
    {
        var command = new ClearNotificationsByCategoryCommand(category!);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Category);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCategoryExceedsMaxLength()
    {
        var command = new ClearNotificationsByCategoryCommand(new string('a', 101));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Category);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenCategoryIsExactly100Characters()
    {
        var command = new ClearNotificationsByCategoryCommand(new string('a', 100));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
