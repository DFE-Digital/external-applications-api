using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Domain.Tests.Aggregates;

public class TemplateTests
{
    [Theory]
    [CustomAutoData(typeof(TemplateCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenIdIsNull(
        string name,
        DateTime createdOn,
        UserId createdBy)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new Template(
                null!,         // id is null
                name,
                createdOn,
                createdBy));

        Assert.Equal("id", ex.ParamName);
    }

    [Theory]
    [CustomAutoData(typeof(TemplateCustomization))]
    public void Constructor_ShouldThrowArgumentNullException_WhenNameIsNull(
        TemplateId id,
        DateTime createdOn,
        UserId createdBy)
    {
        // Act & Assert
        var ex = Assert.Throws<ArgumentNullException>(() =>
            new Template(
                id,
                null!,         // name is null
                createdOn,
                createdBy));

        Assert.Equal("name", ex.ParamName);
    }
}