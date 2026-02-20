using DfE.ExternalApplications.Application.Notifications.Commands;
using FluentValidation.TestHelper;

namespace DfE.ExternalApplications.Application.Tests.CommandValidators.Notifications;

public class ClearNotificationsByContextCommandValidatorTests
{
    private readonly ClearNotificationsByContextCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenContextIsValid()
    {
        var command = new ClearNotificationsByContextCommand("TestContext");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenContextIsEmpty(string? context)
    {
        var command = new ClearNotificationsByContextCommand(context!);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Context);
    }

    [Fact]
    public void Validate_ShouldFail_WhenContextExceedsMaxLength()
    {
        var command = new ClearNotificationsByContextCommand(new string('a', 101));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Context);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenContextIsExactly100Characters()
    {
        var command = new ClearNotificationsByContextCommand(new string('a', 100));

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
