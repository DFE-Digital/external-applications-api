using FluentValidation;
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("DfE.ExternalApplications.Application.Tests")]
namespace DfE.ExternalApplications.Application.Applications.Commands;

internal class AddApplicationResponseCommandValidator : AbstractValidator<AddApplicationResponseCommand>
{
    public AddApplicationResponseCommandValidator()
    {
        RuleFor(x => x.ApplicationId)
            .NotEmpty()
            .WithMessage("Application ID is required");

        RuleFor(x => x.ResponseBody)
            .NotEmpty()
            .WithMessage("Response body is required")
            .Must(BeAValidBase64)
            .WithMessage("ResponseBody must be a valid Base64 encoded string.");
    }
    
    private bool BeAValidBase64(string base64)
    {
        if (string.IsNullOrWhiteSpace(base64))
        {
            return false;
        }
        try
        {
            Convert.FromBase64String(base64);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }
} 