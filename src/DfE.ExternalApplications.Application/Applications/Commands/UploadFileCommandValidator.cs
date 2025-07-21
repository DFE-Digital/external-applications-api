using FluentValidation;

namespace DfE.ExternalApplications.Application.Applications.Commands;

public class UploadFileCommandValidator : AbstractValidator<UploadFileCommand>
{
    public UploadFileCommandValidator()
    {
        RuleFor(x => x.ApplicationId).NotNull();
        RuleFor(x => x.Name).NotEmpty().MaximumLength(255);
        RuleFor(x => x.OriginalFileName).NotEmpty().MaximumLength(255);
        RuleFor(x => x.FileContent).NotNull();
    }
}