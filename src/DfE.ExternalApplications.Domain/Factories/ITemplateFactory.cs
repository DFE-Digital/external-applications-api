using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Factories;

/// <summary>
/// Factory for creating templates and template versions.
/// </summary>
public interface ITemplateFactory
{
    /// <summary>
    /// Creates a new template aggregate.
    /// </summary>
    /// <param name="name">Template display name.</param>
    /// <param name="createdBy">User creating the template.</param>
    /// <param name="tenantId">Owning tenant identifier.</param>
    /// <param name="createdOn">Optional creation timestamp (UTC now when omitted).</param>
    Template CreateTemplate(string name, UserId createdBy, Guid tenantId, DateTime? createdOn = null);

    /// <summary>
    /// Adds a new version to an existing template.
    /// </summary>
    TemplateVersion AddVersionToTemplate(
        Template template,
        string versionNumber,
        string jsonSchema,
        UserId createdBy);
} 