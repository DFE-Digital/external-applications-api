using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GovUK.Dfe.FlexForms.Application.Tests")]
namespace GovUK.Dfe.FlexForms.Application.Applications.Commands;

internal class AddApplicationResponseCommandValidator : AbstractValidator<AddApplicationResponseCommand>
{
    public AddApplicationResponseCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("Application ID is required");

        RuleFor(x => x.ResponseBody)
            .NotEmpty()
            .WithMessage("Response body is required");
    }
} 
