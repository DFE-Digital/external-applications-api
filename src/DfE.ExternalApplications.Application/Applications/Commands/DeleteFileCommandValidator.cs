using FluentValidation;

namespace DfE.ExternalApplications.Application.Applications.Commands;

public class DeleteFileCommandValidator : AbstractValidator<DeleteFileCommand>
{
    public DeleteFileCommandValidator()
    {
        RuleFor(x => x.FileId).NotEmpty();
        RuleFor(x => x.ApplicationId).NotEmpty();
    }
} 