using GovUK.Dfe.FlexForms.Domain.Common;
using FluentValidation;

namespace GovUK.Dfe.FlexForms.Application.Users.Commands;

/// <summary>
/// Validates <see cref="AssignUserRoleCommand"/> requests.
/// </summary>
public sealed class AssignUserRoleCommandValidator : AbstractValidator<AssignUserRoleCommand>
{
    public AssignUserRoleCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(256);

        RuleFor(x => x.Role)
            .NotEmpty()
            .Must(RoleNames.IsAssignable)
            .WithMessage($"Role must be one of: {string.Join(", ", RoleNames.Assignable)}");
    }
}
