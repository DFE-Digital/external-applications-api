using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;

namespace GovUK.Dfe.FlexForms.Domain.Tests.Aggregates;

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