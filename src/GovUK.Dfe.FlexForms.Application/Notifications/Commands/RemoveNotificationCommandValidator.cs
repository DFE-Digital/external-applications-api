using FluentValidation;

namespace GovUK.Dfe.FlexForms.Application.Notifications.Commands;

public class RemoveNotificationCommandValidator : AbstractValidator<RemoveNotificationCommand>
{
    public RemoveNotificationCommandValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty()
            .WithMessage("NotificationId is required");
    }
}
