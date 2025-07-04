using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.Applications.Commands;

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