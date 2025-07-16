using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.Applications.Queries;

internal class GetContributorsForApplicationQueryValidator : AbstractValidator<GetContributorsForApplicationQuery>
{
    public GetContributorsForApplicationQueryValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("Application ID is required");
    }
} 