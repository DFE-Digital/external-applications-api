using System.Runtime.CompilerServices;
using FluentValidation;
using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Request;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]

namespace DfE.ExternalApplications.Application.Applications.Commands;

internal class SubmitUserFeedbackCommandValidator : AbstractValidator<SubmitUserFeedbackCommand>
{
    public SubmitUserFeedbackCommandValidator()
    {
        RuleFor(x => x.Request)
            .SetInheritanceValidator(v =>
            {
                v.Add(new BugReportValidator());
                v.Add(new SupportRequestValidator());
                v.Add(new FeedbackOrSuggestionValidator());
            });
    }
}

internal class BugReportValidator : AbstractValidator<BugReport>
{
    public BugReportValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message is required");

        RuleFor(x => x.EmailAddress)
            .EmailAddress()
            .WithMessage("Email address must be a valid email address");
    }
}

internal class SupportRequestValidator : AbstractValidator<SupportRequest>
{
    public SupportRequestValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message is required");

        RuleFor(x => x.ReferenceNumber)
            .NotEmpty()
            .WithMessage("Reference number is required");

        RuleFor(x => x.EmailAddress)
            .EmailAddress()
            .WithMessage("Email address must be a valid email address");
    }
}

internal class FeedbackOrSuggestionValidator : AbstractValidator<FeedbackOrSuggestion>
{
    public FeedbackOrSuggestionValidator()
    {
        RuleFor(x => x.Message)
            .NotEmpty()
            .WithMessage("Message is required");

        RuleFor(x => x.SatisfactionScore)
            .IsInEnum()
            .WithMessage("Satisfaction score must be a valid value");
    }
}