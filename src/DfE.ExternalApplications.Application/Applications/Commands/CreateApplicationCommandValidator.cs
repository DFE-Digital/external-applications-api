using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.Applications.Commands;

internal class CreateApplicationCommandValidator : AbstractValidator<CreateApplicationCommand>
{
    public CreateApplicationCommandValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotNull()
            .WithMessage("Template ID is required");

        RuleFor(x => x.InitialResponseBody)
            .NotEmpty()
            .WithMessage("Initial response body is required");
    }
} 