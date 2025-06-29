using AutoFixture;
using AutoFixture.Xunit2;
using DfE.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Templates.Queries;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using MediatR;
using MockQueryable.NSubstitute;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

namespace DfE.ExternalApplications.Application.Tests.QueryHandlers.Templates;

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

        var versionQ = new List<TemplateVersion> { version }.AsQueryable().BuildMockDbSet();
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
        var versionQ = new List<TemplateVersion>().AsQueryable().BuildMockDbSet();
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