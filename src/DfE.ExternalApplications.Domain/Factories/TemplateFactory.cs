using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Factories;

public class TemplateFactory : ITemplateFactory
{
    public TemplateVersion AddVersionToTemplate(
        Template template,
        string versionNumber,
        string jsonSchema,
        UserId createdBy)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        if (string.IsNullOrWhiteSpace(versionNumber))
            throw new ArgumentException("Version number cannot be null or empty", nameof(versionNumber));

        if (string.IsNullOrWhiteSpace(jsonSchema))
            throw new ArgumentException("JSON schema cannot be null or empty", nameof(jsonSchema));

        if (createdBy == null)
            throw new ArgumentNullException(nameof(createdBy));

        // Check if version already exists
        if (template.TemplateVersions?.Any(v => v.VersionNumber == versionNumber) == true)
            throw new InvalidOperationException($"Version {versionNumber} already exists for this template");

        var now = DateTime.UtcNow;
        var versionId = new TemplateVersionId(Guid.NewGuid());
        
        var templateVersion = new TemplateVersion(
            versionId,
            template.Id!,
            versionNumber,
            jsonSchema,
            now,
            createdBy);

        template.AddVersion(templateVersion);

        return templateVersion;
    }
} 