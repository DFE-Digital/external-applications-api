using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;

namespace DfE.ExternalApplications.Domain.Factories;

public interface ITemplateFactory
{
    TemplateVersion AddVersionToTemplate(
        Template template,
        string versionNumber,
        string jsonSchema,
        UserId createdBy);
} 