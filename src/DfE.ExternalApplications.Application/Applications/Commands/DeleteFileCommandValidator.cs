using MediatR;
using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;

namespace DfE.ExternalApplications.Application.Applications.Commands;

public class DeleteFileCommandValidator : AbstractValidator<DeleteFileCommand>
{
    public DeleteFileCommandValidator()
    {
        RuleFor(x => x.FileId).NotEmpty();
    }
} 