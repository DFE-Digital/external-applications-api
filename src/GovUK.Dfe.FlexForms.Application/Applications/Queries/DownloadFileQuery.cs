using FluentValidation;

namespace GovUK.Dfe.FlexForms.Application.Applications.Queries;

public class DownloadFileQueryValidator : AbstractValidator<DownloadFileQuery>
{
    public DownloadFileQueryValidator()
    {
        RuleFor(x => x.FileId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();

    }
}
