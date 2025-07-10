using DfE.ExternalApplications.Domain.Common;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Entities;

public sealed class Template : BaseAggregateRoot, IEntity<TemplateId>
{
    private readonly List<TemplateVersion> _templateVersions = new();

    public TemplateId? Id { get; private set; }
    public string Name { get; private set; } = null!;
    public DateTime CreatedOn { get; private set; }
    public UserId CreatedBy { get; private set; }
    public User? CreatedByUser { get; private set; }
    public IReadOnlyCollection<TemplateVersion> TemplateVersions => _templateVersions.AsReadOnly();

    private Template() { /* For EF Core */ }

    /// <summary>
    /// Constructs a new Template.
    /// </summary>
    public Template(
        TemplateId id,
        string name,
        DateTime createdOn,
        UserId createdBy)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        CreatedOn = createdOn;
        CreatedBy = createdBy;
    }

    /// <summary>
    /// Adds a new version to this template.
    /// </summary>
    public void AddVersion(TemplateVersion version)
    {
        if (version == null)
            throw new ArgumentNullException(nameof(version));

        if (version.TemplateId != Id)
            throw new InvalidOperationException("Template version must belong to this template");

        _templateVersions.Add(version);
    }
}
