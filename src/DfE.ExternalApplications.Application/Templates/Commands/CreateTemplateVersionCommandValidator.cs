using FluentValidation;
using System.Text.RegularExpressions;

namespace DfE.ExternalApplications.Application.Templates.Commands;

public sealed class CreateTemplateVersionCommandValidator : AbstractValidator<CreateTemplateVersionCommand>
{
    public CreateTemplateVersionCommandValidator()
    {
        RuleFor(x => x.TemplateId)
            .NotEmpty()
            .WithMessage("Template ID is required");

        RuleFor(x => x.VersionNumber)
            .NotEmpty()
            .WithMessage("Version number is required")
            .Must(BeValidVersionNumber)
            .WithMessage("Version number must be in format X.Y.Z where X, Y, and Z are numbers");

        RuleFor(x => x.JsonSchema)
            .NotEmpty()
            .WithMessage("JSON schema is required")
            .Must(BeValidJson)
            .WithMessage("Invalid JSON schema format");
    }

    private static bool BeValidJson(string jsonSchema)
    {
        try
        {
            System.Text.Json.JsonDocument.Parse(jsonSchema);
            return true;
        }
        catch
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