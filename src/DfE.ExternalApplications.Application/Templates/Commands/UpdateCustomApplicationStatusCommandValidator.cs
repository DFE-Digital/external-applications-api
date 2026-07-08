using FluentValidation;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;

namespace DfE.ExternalApplications.Application.Templates.Commands;

/// <summary>
/// Validates create or update custom application status commands.
/// </summary>
public class UpdateCustomApplicationStatusCommandValidator : AbstractValidator<UpdateCustomApplicationStatusCommand>
{
    public UpdateCustomApplicationStatusCommandValidator()
    {
        RuleFor(x => x.Label)
            .NotEmpty()
            .WithMessage("Label is required");

        RuleFor(x => x.ApplicationStatus)
            .NotNull()
            .WithMessage("ApplicationStatus is required")
            .IsInEnum()
            .WithMessage($"ApplicationStatus must be a valid {nameof(ApplicationStatus)} value");
    }
}
