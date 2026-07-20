using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;

namespace GovUK.Dfe.FlexForms.Domain.Factories;

/// <inheritdoc />
public class TemplateFactory : ITemplateFactory
{
    /// <inheritdoc />
    public Template CreateTemplate(
        string name,
        UserId createdBy,
        Guid tenantId,
        DateTime? createdOn = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Template name cannot be null or empty", nameof(name));

        if (createdBy is null)
            throw new ArgumentNullException(nameof(createdBy));

        return new Template(
            new TemplateId(Guid.NewGuid()),
            name.Trim(),
            createdOn ?? DateTime.UtcNow,
            createdBy,
            tenantId: tenantId);
    }

    /// <inheritdoc />
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
