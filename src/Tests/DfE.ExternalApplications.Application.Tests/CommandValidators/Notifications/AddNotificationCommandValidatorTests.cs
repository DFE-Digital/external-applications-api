using DfE.ExternalApplications.Application.Notifications.Commands;
using FluentValidation.TestHelper;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.ExternalApplications.Application.Tests.CommandValidators.Notifications;

public class AddNotificationCommandValidatorTests
{
    private readonly AddNotificationCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenAllPropertiesValid()
    {
        var command = new AddNotificationCommand("Test message", NotificationType.Info);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenMessageEmpty(string? message)
    {
        var command = new AddNotificationCommand(message!, NotificationType.Info);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Message);
    }

    [Fact]
    public void Validate_ShouldFail_WhenMessageExceedsMaxLength()
    {
        var command = new AddNotificationCommand(new string('a', 1001), NotificationType.Info);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Message);
    }

    [Fact]
    public void Validate_ShouldFail_WhenTypeIsInvalidEnum()
    {
        var command = new AddNotificationCommand("Test", (NotificationType)999);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Type);
    }

    [Fact]
    public void Validate_ShouldFail_WhenCategoryExceedsMaxLength()
    {
        var command = new AddNotificationCommand("Test", NotificationType.Info, Category: new string('a', 101));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Category);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenCategoryIsNull()
    {
        var command = new AddNotificationCommand("Test", NotificationType.Info, Category: null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.Category);
    }

    [Fact]
    public void Validate_ShouldFail_WhenContextExceedsMaxLength()
    {
        var command = new AddNotificationCommand("Test", NotificationType.Info, Context: new string('a', 101));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Context);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAutoDismissSecondsIsZero()
    {
        var command = new AddNotificationCommand("Test", NotificationType.Info, AutoDismissSeconds: 0);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.AutoDismissSeconds);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAutoDismissSecondsIsNegative()
    {
        var command = new AddNotificationCommand("Test", NotificationType.Info, AutoDismissSeconds: -1);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.AutoDismissSeconds);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenAutoDismissSecondsIsNull()
    {
        var command = new AddNotificationCommand("Test", NotificationType.Info, AutoDismissSeconds: null);

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveValidationErrorFor(c => c.AutoDismissSeconds);
    }

    [Fact]
    public void Validate_ShouldFail_WhenPriorityIsInvalidEnum()
    {
        var command = new AddNotificationCommand("Test", NotificationType.Info, Priority: (NotificationPriority)999);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.Priority);
    }

    [Fact]
    public void Validate_ShouldFail_WhenActionUrlExceedsMaxLength()
    {
        var command = new AddNotificationCommand("Test", NotificationType.Info, ActionUrl: new string('a', 501));

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.ActionUrl);
    }

    [Fact]
    public void Validate_ShouldSucceed_WhenAllOptionalFieldsProvided()
    {
        var command = new AddNotificationCommand(
            "Test message",
            NotificationType.Info,
            Category: "TestCategory",
            Context: "TestContext",
            AutoDismissSeconds: 10,
            Priority: NotificationPriority.Normal,
            ActionUrl: "https://example.com");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }
}
