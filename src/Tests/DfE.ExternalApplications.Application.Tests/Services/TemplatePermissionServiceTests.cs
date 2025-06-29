using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Application.TemplatePermissions.Queries;
using DfE.ExternalApplications.Application.Templates.Queries;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using NSubstitute;
using NSubstitute.ReturnsExtensions;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class TemplatePermissionServiceTests
{
    private readonly ISender _mediator;
    private readonly TemplatePermissionService _service;

    public TemplatePermissionServiceTests()
    {
        _mediator = Substitute.For<ISender>();
        _service = new TemplatePermissionService(_mediator);
    }

    [Fact]
    public async Task CanUserCreateApplicationForTemplate_ShouldReturnTrue_WhenUserHasWritePermission()
    {
        // Arrange
        var email = "test@example.com";
        var templateId = new TemplateId(Guid.NewGuid());
        var permissions = new List<TemplatePermissionDto>
        {
            new() { TemplateId = templateId, PermissionType = PermissionType.Write }
        };

        _mediator.Send(Arg.Any<GetTemplatePermissionsForUserQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyCollection<TemplatePermissionDto>>.Success(permissions));

        // Act
        var result = await _service.CanUserCreateApplicationForTemplate(email, templateId);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserCreateApplicationForTemplate_ShouldReturnFalse_WhenUserHasNoWritePermission()
    {
        // Arrange
        var email = "test@example.com";
        var templateId = new TemplateId(Guid.NewGuid());
        var permissions = new List<TemplatePermissionDto>
        {
            new() { TemplateId = templateId, PermissionType = PermissionType.Read }
        };

        _mediator.Send(Arg.Any<GetTemplatePermissionsForUserQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyCollection<TemplatePermissionDto>>.Success(permissions));

        // Act
        var result = await _service.CanUserCreateApplicationForTemplate(email, templateId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task CanUserCreateApplicationForTemplate_ShouldReturnFalse_WhenPermissionCheckFails()
    {
        // Arrange
        var email = "test@example.com";
        var templateId = new TemplateId(Guid.NewGuid());

        _mediator.Send(Arg.Any<GetTemplatePermissionsForUserQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyCollection<TemplatePermissionDto>>.Failure("Error"));

        // Act
        var result = await _service.CanUserCreateApplicationForTemplate(email, templateId);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public async Task GetLatestAccessibleTemplateVersion_ShouldReturnVersion_WhenUserHasAccess()
    {
        // Arrange
        var email = "test@example.com";
        var templateId = new TemplateId(Guid.NewGuid());
        var versionId = new TemplateVersionId(Guid.NewGuid());
        var template = new TemplateSchemaDto { TemplateVersionId = versionId };

        _mediator.Send(Arg.Any<GetLatestTemplateSchemaQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TemplateSchemaDto>.Success(template));

        // Act
        var result = await _service.GetLatestAccessibleTemplateVersion(email, templateId);

        // Assert
        Assert.Equal(versionId, result);
    }

    [Fact]
    public async Task GetLatestAccessibleTemplateVersion_ShouldReturnNull_WhenUserHasNoAccess()
    {
        // Arrange
        var email = "test@example.com";
        var templateId = new TemplateId(Guid.NewGuid());

        _mediator.Send(Arg.Any<GetLatestTemplateSchemaQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<TemplateSchemaDto>.Failure("Error"));

        // Act
        var result = await _service.GetLatestAccessibleTemplateVersion(email, templateId);

        // Assert
        Assert.Null(result);
    }
} 