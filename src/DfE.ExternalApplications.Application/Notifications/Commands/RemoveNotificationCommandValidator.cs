using FluentValidation;

namespace DfE.ExternalApplications.Application.Notifications.Commands;

public class RemoveNotificationCommandValidator : AbstractValidator<RemoveNotificationCommand>
{
    public RemoveNotificationCommandValidator()
    {
        RuleFor(x => x.NotificationId)
            .NotEmpty()
            .WithMessage("NotificationId is required");
    }
}
