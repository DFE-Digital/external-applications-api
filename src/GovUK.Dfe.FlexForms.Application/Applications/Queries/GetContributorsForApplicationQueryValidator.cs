using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("GovUK.Dfe.FlexForms.Application.Tests")]
namespace GovUK.Dfe.FlexForms.Application.Applications.Queries;

internal class GetContributorsForApplicationQueryValidator : AbstractValidator<GetContributorsForApplicationQuery>
{
    public GetContributorsForApplicationQueryValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("Application ID is required");
    }
} 
