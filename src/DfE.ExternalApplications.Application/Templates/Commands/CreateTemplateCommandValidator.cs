using FluentValidation;

namespace DfE.ExternalApplications.Application.Templates.Commands;

/// <summary>
/// Validates <see cref="CreateTemplateCommand"/>.
/// </summary>
public sealed class CreateTemplateCommandValidator : AbstractValidator<CreateTemplateCommand>
{
    public CreateTemplateCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x)
            .Must(x =>
                (string.IsNullOrWhiteSpace(x.InitialVersionNumber) && string.IsNullOrWhiteSpace(x.JsonSchema))
                || (!string.IsNullOrWhiteSpace(x.InitialVersionNumber) && !string.IsNullOrWhiteSpace(x.JsonSchema)))
            .WithMessage("InitialVersionNumber and JsonSchema must both be provided together, or both omitted.");

        When(x => !string.IsNullOrWhiteSpace(x.InitialVersionNumber), () =>
        {
            RuleFor(x => x.InitialVersionNumber!)
                .MaximumLength(50);
        });
    }
}
