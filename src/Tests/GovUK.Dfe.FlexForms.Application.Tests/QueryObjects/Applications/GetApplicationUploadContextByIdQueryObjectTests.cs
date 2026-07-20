using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Application.Applications.QueryObjects;
using GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using MockQueryable;
using ApplicationId = GovUK.Dfe.FlexForms.Domain.ValueObjects.ApplicationId;

namespace GovUK.Dfe.FlexForms.Application.Tests.QueryObjects.Applications;

public class GetApplicationUploadContextByIdQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnUploadContext_WhenApplicationIdExists(ApplicationCustomization appCustom)
    {
        // Arrange
        var targetApplicationId = new ApplicationId(Guid.NewGuid());

        var fixture = new Fixture().Customize(appCustom);
        var targetApplication = fixture.Create<Domain.Entities.Application>();

        var idProperty = typeof(Domain.Entities.Application).GetProperty("Id");
        idProperty?.SetValue(targetApplication, targetApplicationId);

        var applications = new[] { targetApplication };
        var queryable = applications.AsQueryable().BuildMock();

        var queryObject = new GetApplicationUploadContextByIdQueryObject(targetApplicationId);

        // Act
        var result = queryObject.Apply(queryable).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(targetApplication.ApplicationReference, result.First().ApplicationReference);
    }

    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnEmpty_WhenApplicationIdDoesNotExist(ApplicationCustomization appCustom)
    {
        // Arrange
        var targetApplicationId = new ApplicationId(Guid.NewGuid());
        var otherApplicationId = new ApplicationId(Guid.NewGuid());

        var fixture = new Fixture().Customize(appCustom);
        var application = fixture.Create<Domain.Entities.Application>();

        var idProperty = typeof(Domain.Entities.Application).GetProperty("Id");
        idProperty?.SetValue(application, otherApplicationId);

        var applications = new[] { application };
        var queryable = applications.AsQueryable().BuildMock();

        var queryObject = new GetApplicationUploadContextByIdQueryObject(targetApplicationId);

        // Act
        var result = queryObject.Apply(queryable).ToList();

        // Assert
        Assert.Empty(result);
    }

    [Fact]
    public void Apply_ShouldReturnEmpty_WhenNoApplicationsExist()
    {
        // Arrange
        var targetApplicationId = new ApplicationId(Guid.NewGuid());
        var applications = Array.Empty<Domain.Entities.Application>();
        var queryable = applications.AsQueryable().BuildMock();

        var queryObject = new GetApplicationUploadContextByIdQueryObject(targetApplicationId);

        // Act
        var result = queryObject.Apply(queryable).ToList();

        // Assert
        Assert.Empty(result);
    }
}
