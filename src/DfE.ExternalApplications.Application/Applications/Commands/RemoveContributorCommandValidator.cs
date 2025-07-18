using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.Applications.Commands;

internal class RemoveContributorCommandValidator : AbstractValidator<RemoveContributorCommand>
{
    public RemoveContributorCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("Application ID is required");

        RuleFor(x => x.UserId)
            .NotEmpty()
            .WithMessage("User ID is required");
    }
} 