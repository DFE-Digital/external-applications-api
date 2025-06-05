using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Domain.Entities;

public sealed class Application : BaseAggregateRoot, IEntity<ApplicationId>
{
    public ApplicationId? Id { get; private set; }
    public string ApplicationReference { get; private set; }
    public TemplateVersionId TemplateVersionId { get; private set; }
    public TemplateVersion? TemplateVersion { get; private set; }
    public DateTime CreatedOn { get; private set; }
    public UserId CreatedBy { get; private set; }
    public User? CreatedByUser { get; private set; }
    public int? Status { get; private set; }
    public DateTime? LastModifiedOn { get; private set; }
    public UserId? LastModifiedBy { get; private set; }
    public User? LastModifiedByUser { get; private set; }

    private Application() { /* For EF Core */ }

    /// <summary>
    /// Constructs a new Application.
    /// Pass null for optional fields (Status, LastModifiedOn, LastModifiedBy).
    /// </summary>
    public Application(
        ApplicationId id,
        string applicationReference,
        TemplateVersionId templateVersionId,
        DateTime createdOn,
        UserId createdBy,
        int? status = null,
        DateTime? lastModifiedOn = null,
        UserId? lastModifiedBy = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        ApplicationReference = applicationReference?.Trim()
                               ?? throw new ArgumentNullException(nameof(applicationReference));
        TemplateVersionId = templateVersionId;
        CreatedOn = createdOn;
        CreatedBy = createdBy;
        Status = status;
        LastModifiedOn = lastModifiedOn;
        LastModifiedBy = lastModifiedBy;
    }
}