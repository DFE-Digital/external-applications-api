using FluentValidation;
using System.Text.RegularExpressions;

namespace DfE.ExternalApplications.Application.Templates.Commands;

public class CreateTemplateVersionCommandValidator : AbstractValidator<CreateTemplateVersionCommand>
{
    public CreateTemplateVersionCommandValidator()
    {
        RuleFor(v => v.TemplateId)
            .NotEmpty();

        RuleFor(v => v.VersionNumber)
            .NotEmpty()
            .Must(BeValidVersionNumber)
            .WithMessage("Invalid Template Version format.");

        RuleFor(v => v.JsonSchema)
            .NotEmpty()
            .Must(BeValidBase64)
            .WithMessage("JsonSchema must be a valid Base64 encoded string.");
    }

    private bool BeValidBase64(string base64)
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

    private static bool BeValidVersionNumber(string versionNumber)
    {
        if (string.IsNullOrWhiteSpace(versionNumber))
            return false;

        var match = Regex.Match(versionNumber, @"^\d+\.\d+\.\d+$");
        return match.Success;
    }
}