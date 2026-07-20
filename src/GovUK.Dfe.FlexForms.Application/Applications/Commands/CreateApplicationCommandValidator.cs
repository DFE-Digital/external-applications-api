using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GovUK.Dfe.FlexForms.Application.Tests")]
namespace GovUK.Dfe.FlexForms.Application.Applications.Commands;

internal class CreateApplicationCommandValidator : AbstractValidator<CreateApplicationCommand>
{
    public CreateApplicationCommandValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty()
            .WithMessage("Template ID is required");

        RuleFor(x => x.InitialResponseBody)
            .NotEmpty()
            .WithMessage("Initial response body is required");
    }
} 
