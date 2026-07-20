using AutoFixture;
using AutoFixture.Xunit2;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using GovUK.Dfe.FlexForms.Application.Templates.Queries;
using GovUK.Dfe.FlexForms.Domain.Entities;
using GovUK.Dfe.FlexForms.Domain.Interfaces.Repositories;
using GovUK.Dfe.FlexForms.Domain.ValueObjects;
using GovUK.Dfe.FlexForms.Tests.Common.Customizations.Entities;
using MediatR;
using MockQueryable;
using MockQueryable.NSubstitute;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace GovUK.Dfe.FlexForms.Application.Tests.QueryHandlers.Templates;

public class GetLatestTemplateSchemaByUserIdQueryHandlerTests
{
    [Theory]
    [CustomAutoData(typeof(TemplateVersionCustomization))]
    public async Task Handle_ShouldReturnLatestSchema_WhenVersionExists(
        TemplateVersionCustomization tvCustom,
        [Frozen] IEaRepository<TemplateVersion> versionRepo,
        [Frozen] ISender mediator)
    {
        // Arrange
        var template = new Fixture().Customize(new TemplateCustomization()).Create<Template>();
        var version = new Fixture().Customize(tvCustom).Create<TemplateVersion>();

        // Set up template version
        version.GetType().GetProperty(nameof(TemplateVersion.Template))!.SetValue(version, template);
        version.GetType().GetProperty(nameof(TemplateVersion.TemplateId))!.SetValue(version, template.Id);

        var versionQ = new List<TemplateVersion> { version }.AsQueryable().BuildMock();
        versionRepo.Query().Returns(versionQ);

        var handler = new GetLatestTemplateSchemaByUserIdQueryHandler(versionRepo, mediator);

        // Act
        var result = await handler.Handle(
            new GetLatestTemplateSchemaByUserIdQuery(template.Id.Value, new UserId(Guid.NewGuid())),
            CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(version.Id!.Value, result.Value.TemplateVersionId);
        Assert.Equal(version.JsonSchema, result.Value.JsonSchema);
        Assert.Equal(version.TemplateId.Value, result.Value.TemplateId);
        Assert.Equal(version.VersionNumber, result.Value.VersionNumber);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnFailure_WhenNoVersionsExist(
        [Frozen] IEaRepository<TemplateVersion> versionRepo,
        [Frozen] ISender mediator)
    {
        // Arrange
        var versionQ = new List<TemplateVersion>().AsQueryable().BuildMock();
        versionRepo.Query().Returns(versionQ);

        var handler = new GetLatestTemplateSchemaByUserIdQueryHandler(versionRepo, mediator);

        // Act
        var result = await handler.Handle(
            new GetLatestTemplateSchemaByUserIdQuery(Guid.NewGuid(), new UserId(Guid.NewGuid())),
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Template version not found", result.Error);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ShouldReturnFailure_WhenExceptionOccurs(
        Exception exception,
        [Frozen] IEaRepository<TemplateVersion> versionRepo,
        [Frozen] ISender mediator)
    {
        // Arrange
        versionRepo.Query().Throws(exception);

        var handler = new GetLatestTemplateSchemaByUserIdQueryHandler(versionRepo, mediator);

        // Act
        var result = await handler.Handle(
            new GetLatestTemplateSchemaByUserIdQuery(Guid.NewGuid(), new UserId(Guid.NewGuid())),
            CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(exception.ToString(), result.Error);
    }
} 