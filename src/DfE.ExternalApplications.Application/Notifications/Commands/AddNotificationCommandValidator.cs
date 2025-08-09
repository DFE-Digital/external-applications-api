using FluentValidation;

namespace DfE.ExternalApplications.Application.Notifications.Commands;

public class AddNotificationCommandValidator : AbstractValidator<AddNotificationCommand>
{
    public AddNotificationCommandValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message is required")
            .MaximumLength(1000)
            .WithMessage("Message cannot exceed 1000 characters");

        RuleFor(x => x.Type)
            .IsInEnum()
            .WithMessage("Type must be a valid NotificationType");

        RuleFor(x => x.Category)
            .MaximumLength(100)
            .WithMessage("Category cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Category));

        RuleFor(x => x.Context)
            .MaximumLength(100)
            .WithMessage("Context cannot exceed 100 characters")
            .When(x => !string.IsNullOrEmpty(x.Context));

        RuleFor(x => x.AutoDismissSeconds)
            .GreaterThan(0)
            .WithMessage("AutoDismissSeconds must be greater than 0")
            .When(x => x.AutoDismissSeconds.HasValue);

        RuleFor(x => x.Priority)
            .IsInEnum()
            .WithMessage("Priority must be a valid NotificationPriority")
            .When(x => x.Priority.HasValue);

        RuleFor(x => x.ActionUrl)
            .MaximumLength(500)
            .WithMessage("ActionUrl cannot exceed 500 characters")
            .When(x => !string.IsNullOrEmpty(x.ActionUrl));
    }
}
