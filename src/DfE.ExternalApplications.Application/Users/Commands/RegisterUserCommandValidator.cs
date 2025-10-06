using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.Users.Commands;

internal class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    public RegisterUserCommandValidator()
    {
        RuleFor(x => x.SubjectToken)
            .NotEmpty()
            .WithMessage("Subject token is required");
        
        RuleFor(x => x.TemplateId)
            .NotNull()
            .NotEmpty()
            .WithMessage("Template ID is required");
    }
}

