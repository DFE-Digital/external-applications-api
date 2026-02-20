using AutoFixture;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Applications.QueryObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using DfE.ExternalApplications.Domain.ValueObjects;
using MockQueryable;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Tests.QueryObjects.Applications;

public class GetApplicationByIdForResponseWriteQueryObjectTests
{
    [Theory]
    [CustomAutoData(typeof(ApplicationCustomization))]
    public void Apply_ShouldReturnMatchingApplication_WhenApplicationIdExists(ApplicationCustomization appCustom)
    {
        // Arrange
        var targetApplicationId = new ApplicationId(Guid.NewGuid());
        var otherApplicationId = new ApplicationId(Guid.NewGuid());

        var fixture = new Fixture().Customize(appCustom);
        var targetApplication = fixture.Create<Domain.Entities.Application>();
        var otherApplication = fixture.Create<Domain.Entities.Application>();

        var idProperty = typeof(Domain.Entities.Application).GetProperty("Id");
        idProperty?.SetValue(targetApplication, targetApplicationId);
        idProperty?.SetValue(otherApplication, otherApplicationId);

        var applications = new[] { targetApplication, otherApplication };
        var queryable = applications.AsQueryable().BuildMock();

        var queryObject = new GetApplicationByIdForResponseWriteQueryObject(targetApplicationId);

        // Act
        var result = queryObject.Apply(queryable).ToList();

        // Assert
        Assert.Single(result);
        Assert.Equal(targetApplication, result.First());
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

        var queryObject = new GetApplicationByIdForResponseWriteQueryObject(targetApplicationId);

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

        var queryObject = new GetApplicationByIdForResponseWriteQueryObject(targetApplicationId);

        // Act
        var result = queryObject.Apply(queryable).ToList();

        // Assert
        Assert.Empty(result);
    }
}
