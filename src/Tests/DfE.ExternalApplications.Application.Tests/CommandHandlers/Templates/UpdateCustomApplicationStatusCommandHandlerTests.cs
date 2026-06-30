using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Enums;
using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Templates.Commands;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Interfaces;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using DfE.ExternalApplications.Domain.ValueObjects;
using Microsoft.AspNetCore.Http;
using MockQueryable;
using MockQueryable.NSubstitute;
using NSubstitute;
using System.Security.Claims;
using AutoFixture;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;

namespace DfE.ExternalApplications.Application.Tests.CommandHandlers.Templates;

public class UpdateCustomApplicationStatusCommandHandlerTests
{
    [Theory]
    [CustomAutoData]
    public async Task Handle_UpdatesLabel_WhenCustomStatusExists(
        Guid templateId,
        string newLabel)
    {
        // Arrange
        var customStatusRepo = Substitute.For<IEaRepository<CustomApplicationStatus>>();
        var userRepo = Substitute.For<IEaRepository<User>>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var existingStatus = new CustomApplicationStatus(
            new CustomApplicationStatusId(Guid.NewGuid()),
            new TemplateId(templateId),
            ApplicationStatus.Submitted,
            "Old Label",
            DateTime.UtcNow,
            new UserId(Guid.NewGuid())
        );

        var statusQueryable = new List<CustomApplicationStatus> { existingStatus }.AsQueryable().BuildMock();
        customStatusRepo.Query().Returns(statusQueryable);

        var handler = new UpdateCustomApplicationStatusCommandHandler(
            customStatusRepo,
            userRepo,
            httpContextAccessor,
            unitOfWork);

        var command = new UpdateCustomApplicationStatusCommand(templateId, ApplicationStatus.Submitted, newLabel);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(newLabel, result.Value.Label);
        Assert.Equal(ApplicationStatus.Submitted, result.Value.ApplicationStatus);
        await unitOfWork.Received(1).CommitAsync(CancellationToken.None);
        await customStatusRepo.DidNotReceive().AddAsync(Arg.Any<CustomApplicationStatus>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ReturnsForbid_WhenUserNotAuthenticated(
        Guid templateId,
        string label)
    {
        // Arrange
        var customStatusRepo = Substitute.For<IEaRepository<CustomApplicationStatus>>();
        var userRepo = Substitute.For<IEaRepository<User>>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        // No existing custom status
        var emptyStatusList = new List<CustomApplicationStatus>().AsQueryable().BuildMock();
        customStatusRepo.Query().Returns(emptyStatusList);

        // Unauthenticated user
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity());
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new UpdateCustomApplicationStatusCommandHandler(
            customStatusRepo,
            userRepo,
            httpContextAccessor,
            unitOfWork);

        var command = new UpdateCustomApplicationStatusCommand(templateId, ApplicationStatus.Submitted, label);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Not authenticated", result.Error);
        await customStatusRepo.DidNotReceive().AddAsync(Arg.Any<CustomApplicationStatus>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(CancellationToken.None);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_ReturnsForbid_WhenUserNotFoundInDatabase(
        Guid templateId,
        string label,
        string userEmail)
    {
        // Arrange
        var customStatusRepo = Substitute.For<IEaRepository<CustomApplicationStatus>>();
        var userRepo = Substitute.For<IEaRepository<User>>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        // No existing custom status
        var emptyStatusList = new List<CustomApplicationStatus>().AsQueryable().BuildMock();
        customStatusRepo.Query().Returns(emptyStatusList);

        // User not found in database
        var emptyUserList = new List<User>().AsQueryable().BuildMock();
        userRepo.Query().Returns(emptyUserList);

        var httpContext = new DefaultHttpContext();
        var claims = new List<Claim> { new(ClaimTypes.Email, userEmail) };
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        httpContextAccessor.HttpContext.Returns(httpContext);

        var handler = new UpdateCustomApplicationStatusCommandHandler(
            customStatusRepo,
            userRepo,
            httpContextAccessor,
            unitOfWork);

        var command = new UpdateCustomApplicationStatusCommand(templateId, ApplicationStatus.Submitted, label);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Unable to resolve CreatedBy user", result.Error);
        await customStatusRepo.DidNotReceive().AddAsync(Arg.Any<CustomApplicationStatus>(), Arg.Any<CancellationToken>());
        await unitOfWork.DidNotReceive().CommitAsync(CancellationToken.None);
    }

    [Theory]
    [CustomAutoData]
    public async Task Handle_AllowsNullLabel_WhenUpdating(
        Guid templateId)
    {
        // Arrange
        var customStatusRepo = Substitute.For<IEaRepository<CustomApplicationStatus>>();
        var userRepo = Substitute.For<IEaRepository<User>>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var unitOfWork = Substitute.For<IUnitOfWork>();

        var existingStatus = new CustomApplicationStatus(
            new CustomApplicationStatusId(Guid.NewGuid()),
            new TemplateId(templateId),
            ApplicationStatus.Submitted,
            "Old Label",
            DateTime.UtcNow,
            new UserId(Guid.NewGuid())
        );

        var statusQueryable = new List<CustomApplicationStatus> { existingStatus }.AsQueryable().BuildMock();
        customStatusRepo.Query().Returns(statusQueryable);

        var handler = new UpdateCustomApplicationStatusCommandHandler(
            customStatusRepo,
            userRepo,
            httpContextAccessor,
            unitOfWork);

        var command = new UpdateCustomApplicationStatusCommand(templateId, ApplicationStatus.Submitted, null);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.Label);
        await unitOfWork.Received(1).CommitAsync(CancellationToken.None);
    }
}
