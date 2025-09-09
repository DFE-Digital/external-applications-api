using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Domain.Tests.Aggregates;

public class TemplateVersionTests
{
    [Theory]
    [CustomAutoData(typeof(TemplateVersionCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenIdIsNull(
        TemplateId templateId,
        string versionNumber,
        string jsonSchema,
        DateTime createdOn,
        UserId createdBy,
        DateTime? lastModifiedOn,
        UserId? lastModifiedBy)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new TemplateVersion(
                null!,            // id is null
                templateId,
                versionNumber,
                jsonSchema,
                createdOn,
                createdBy,
                lastModifiedOn,
                lastModifiedBy));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(TemplateVersionCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenVersionNumberIsNull(
        TemplateVersionId id,
        TemplateId templateId,
        string jsonSchema,
        DateTime createdOn,
        UserId createdBy,
        DateTime? lastModifiedOn,
        UserId? lastModifiedBy)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new TemplateVersion(
                id,
                templateId,
                null!,           // versionNumber is null
                jsonSchema,
                createdOn,
                createdBy,
                lastModifiedOn,
                lastModifiedBy));

        Assert.Equal("versionNumber", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(TemplateVersionCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenJsonSchemaIsNull(
        TemplateVersionId id,
        TemplateId templateId,
        string versionNumber,
        DateTime createdOn,
        UserId createdBy,
        DateTime? lastModifiedOn,
        UserId? lastModifiedBy)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new TemplateVersion(
                id,
                templateId,
                versionNumber,
                null!,          // jsonSchema is null
                createdOn,
                createdBy,
                lastModifiedOn,
                lastModifiedBy));

        Assert.Equal("jsonSchema", ex.ParamName);
    }
}