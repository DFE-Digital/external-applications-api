using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.CoreLibs.Contracts.ExternalApplications.Models.Response;
using DfE.ExternalApplications.Application.Services;
using DfE.ExternalApplications.Application.TemplatePermissions.Queries;
using DfE.ExternalApplications.Domain.ValueObjects;
using MediatR;
using NSubstitute;

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
        var userId = Guid.NewGuid();

        var permissions = new List<TemplatePermissionDto>
        {
            new() { TemplateId = templateId.Value, UserId = userId, AccessType = AccessType.Write }
        };

        _mediator.Send(Arg.Any<GetTemplatePermissionsForUserQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyCollection<TemplatePermissionDto>>.Success(permissions));

        // Act
        var result = await _service.CanUserCreateApplicationForTemplate(email, templateId.Value);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public async Task CanUserCreateApplicationForTemplate_ShouldReturnFalse_WhenUserHasNoWritePermission()
    {
        // Arrange
        var email = "test@example.com";
        var templateId = new TemplateId(Guid.NewGuid());
        var userId = Guid.NewGuid();

        var permissions = new List<TemplatePermissionDto>
        {
            new() { TemplateId = templateId.Value, UserId = userId, AccessType = AccessType.Read }
        };

        _mediator.Send(Arg.Any<GetTemplatePermissionsForUserQuery>(), Arg.Any<CancellationToken>())
            .Returns(Result<IReadOnlyCollection<TemplatePermissionDto>>.Success(permissions));

        // Act
        var result = await _service.CanUserCreateApplicationForTemplate(email, templateId.Value);

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
        var result = await _service.CanUserCreateApplicationForTemplate(email, templateId.Value);

        // Assert
        Assert.False(result);
    }
} 