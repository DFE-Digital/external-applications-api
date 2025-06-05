using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Entities;

public sealed class TemplateVersion : IEntity<TemplateVersionId>
{
    public TemplateVersionId? Id { get; private set; }
    public TemplateId TemplateId { get; private set; }
    public Template? Template { get; private set; }
    public string VersionNumber { get; private set; } = null!;
    public string JsonSchema { get; private set; } = null!;
    public DateTime CreatedOn { get; private set; }
    public UserId CreatedBy { get; private set; }
    public User? CreatedByUser { get; private set; }
    public DateTime? LastModifiedOn { get; private set; }
    public UserId? LastModifiedBy { get; private set; }
    public User? LastModifiedByUser { get; private set; }

    private TemplateVersion() { /* For EF Core */ }

    /// <summary>
    /// Constructs a new TemplateVersion.
    /// </summary>
    public TemplateVersion(
        TemplateVersionId id,
        TemplateId templateId,
        string versionNumber,
        string jsonSchema,
        DateTime createdOn,
        UserId createdBy,
        DateTime? lastModifiedOn = null,
        UserId? lastModifiedBy = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        TemplateId = templateId;
        VersionNumber = versionNumber ?? throw new ArgumentNullException(nameof(versionNumber));
        JsonSchema = jsonSchema ?? throw new ArgumentNullException(nameof(jsonSchema));
        CreatedOn = createdOn;
        CreatedBy = createdBy;
        LastModifiedOn = lastModifiedOn;
        LastModifiedBy = lastModifiedBy;
    }
}