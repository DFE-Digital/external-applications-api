using FluentValidation;

namespace GovUK.Dfe.FlexForms.Application.Applications.Queries;

public class GetFilesForApplicationQueryValidator : AbstractValidator<GetFilesForApplicationQuery>
{
    public GetFilesForApplicationQueryValidator()
    {
        RuleFor(x => x.ApplicationId).NotNull();
    }
}
