using FluentValidation;

namespace DfE.ExternalApplications.Application.Applications.Queries;

public class GetUploadsForApplicationQueryValidator : AbstractValidator<GetUploadsForApplicationQuery>
{
    public GetUploadsForApplicationQueryValidator()
    {
        RuleFor(x => x.ApplicationId).NotNull();
    }
}
