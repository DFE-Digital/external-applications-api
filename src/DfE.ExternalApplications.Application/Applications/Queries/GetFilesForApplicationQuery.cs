using FluentValidation;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public class GetFilesForApplicationQueryValidator : AbstractValidator<GetFilesForApplicationQuery>
{
    public GetFilesForApplicationQueryValidator()
    {
        RuleFor(x => x.ApplicationId).NotNull();
    }
}
