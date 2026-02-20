using DfE.ExternalApplications.Application.Notifications.Commands;
using FluentValidation.TestHelper;

namespace DfE.ExternalApplications.Application.Tests.CommandValidators.Notifications;

public class MarkNotificationAsReadCommandValidatorTests
{
    private readonly MarkNotificationAsReadCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldSucceed_WhenNotificationIdIsValid()
    {
        var command = new MarkNotificationAsReadCommand("notification-123");

        var result = _validator.TestValidate(command);

        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void Validate_ShouldFail_WhenNotificationIdIsEmpty(string? notificationId)
    {
        var command = new MarkNotificationAsReadCommand(notificationId!);

        var result = _validator.TestValidate(command);

        result.ShouldHaveValidationErrorFor(c => c.NotificationId);
    }
}
