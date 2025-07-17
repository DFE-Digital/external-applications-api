using DfE.ExternalApplications.Application.Applications.EventHandlers;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Domain.Factories;
using DfE.ExternalApplications.Domain.ValueObjects;
using DfE.CoreLibs.Contracts.ExternalApplications.Enums;
using DfE.ExternalApplications.Domain.Interfaces.Repositories;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;
using ApplicationId = DfE.ExternalApplications.Domain.ValueObjects.ApplicationId;

namespace DfE.ExternalApplications.Application.Tests.EventHandlers;

public class ContributorAddedEventHandlerTests
{
    private readonly ContributorAddedEventHandler _handler;
    private readonly IUserFactory _userFactory;
    private readonly ILogger<ContributorAddedEventHandler> _logger;
    private readonly IEaRepository<User> _userRepo;

    public ContributorAddedEventHandlerTests()
    {
        _userFactory = Substitute.For<IUserFactory>();
        _logger = Substitute.For<ILogger<ContributorAddedEventHandler>>();
        _userRepo = Substitute.For<IEaRepository<User>>();
        _handler = new ContributorAddedEventHandler(_logger, _userRepo, _userFactory);
    }

    [Fact]
    public async Task Handle_WithValidEvent_ShouldAddPermissionsToUser()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var createdBy = new UserId(Guid.NewGuid());
        var createdOn = DateTime.UtcNow;

        var @event = new ContributorAddedEvent(applicationId, user, createdBy, createdOn);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _userFactory.Received(1).AddPermissionToUser(
            user,
            applicationId.Value.ToString(),
            ResourceType.Application,
            Arg.Is<AccessType[]>(accessTypes => accessTypes.Length == 2 && accessTypes.Contains(AccessType.Read) && accessTypes.Contains(AccessType.Write)),
            createdBy,
            applicationId,
            createdOn);
    }

    [Fact]
    public async Task Handle_WithNullEvent_ShouldThrowArgumentNullException()
    {
        // Arrange
        ContributorAddedEvent @event = null!;

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(@event, CancellationToken.None));
        Assert.Contains("event", exception.Message);
    }

    [Fact]
    public async Task Handle_WithNullUser_ShouldThrowArgumentNullException()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        User user = null!;
        var createdBy = new UserId(Guid.NewGuid());
        var createdOn = DateTime.UtcNow;

        var @event = new ContributorAddedEvent(applicationId, user, createdBy, createdOn);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(@event, CancellationToken.None));
        Assert.Contains("event", exception.Message);
    }

    [Fact]
    public async Task Handle_WithNullApplicationId_ShouldThrowArgumentNullException()
    {
        // Arrange
        ApplicationId applicationId = null!;
        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var createdBy = new UserId(Guid.NewGuid());
        var createdOn = DateTime.UtcNow;

        var @event = new ContributorAddedEvent(applicationId, user, createdBy, createdOn);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(@event, CancellationToken.None));
        Assert.Contains("event", exception.Message);
    }

    [Fact]
    public async Task Handle_WithNullCreatedBy_ShouldThrowArgumentNullException()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        UserId createdBy = null!;
        var createdOn = DateTime.UtcNow;

        var @event = new ContributorAddedEvent(applicationId, user, createdBy, createdOn);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _handler.Handle(@event, CancellationToken.None));
        Assert.Contains("event", exception.Message);
    }

    [Fact]
    public async Task Handle_WithFactoryThrowingException_ShouldPropagateException()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var createdBy = new UserId(Guid.NewGuid());
        var createdOn = DateTime.UtcNow;

        var @event = new ContributorAddedEvent(applicationId, user, createdBy, createdOn);

        _userFactory.When(x => x.AddPermissionToUser(Arg.Any<User>(), Arg.Any<string>(), Arg.Any<ResourceType>(), Arg.Any<AccessType[]>(), Arg.Any<UserId>(), Arg.Any<ApplicationId>(), Arg.Any<DateTime?>()))
            .Do(x => { throw new InvalidOperationException("Factory error"); });

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _handler.Handle(@event, CancellationToken.None));
        Assert.Contains("Factory error", exception.Message);
    }

    [Fact]
    public async Task Handle_WithCancelledToken_ShouldThrowOperationCancelledException()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var createdBy = new UserId(Guid.NewGuid());
        var createdOn = DateTime.UtcNow;

        var @event = new ContributorAddedEvent(applicationId, user, createdBy, createdOn);
        var cancellationToken = new CancellationToken(true); // Cancelled token

        // Act & Assert
        await Assert.ThrowsAsync<OperationCanceledException>(() => _handler.Handle(@event, cancellationToken));
    }

    [Fact]
    public async Task Handle_WithMultipleEvents_ShouldCallFactoryForEachEvent()
    {
        // Arrange
        var applicationId1 = new ApplicationId(Guid.NewGuid());
        var applicationId2 = new ApplicationId(Guid.NewGuid());
        var user1 = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "User 1", "user1@example.com", DateTime.UtcNow, null, null, null);
        var user2 = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "User 2", "user2@example.com", DateTime.UtcNow, null, null, null);
        var createdBy = new UserId(Guid.NewGuid());
        var createdOn = DateTime.UtcNow;

        var event1 = new ContributorAddedEvent(applicationId1, user1, createdBy, createdOn);
        var event2 = new ContributorAddedEvent(applicationId2, user2, createdBy, createdOn);

        // Act
        await _handler.Handle(event1, CancellationToken.None);
        await _handler.Handle(event2, CancellationToken.None);

        // Assert
        _userFactory.Received(2).AddPermissionToUser(
            Arg.Any<User>(),
            Arg.Any<string>(),
            ResourceType.Application,
            Arg.Is<AccessType[]>(accessTypes => accessTypes.Length == 2 && accessTypes.Contains(AccessType.Read) && accessTypes.Contains(AccessType.Write)),
            createdBy,
            Arg.Any<ApplicationId>(),
            createdOn);
    }

    [Fact]
    public async Task Handle_WithIdempotentCalls_ShouldNotThrowException()
    {
        // Arrange
        var applicationId = new ApplicationId(Guid.NewGuid());
        var user = new User(new UserId(Guid.NewGuid()), new RoleId(Guid.NewGuid()), "Test User", "test@example.com", DateTime.UtcNow, null, null, null);
        var createdBy = new UserId(Guid.NewGuid());
        var createdOn = DateTime.UtcNow;

        var @event = new ContributorAddedEvent(applicationId, user, createdBy, createdOn);

        // Act & Assert - should not throw on multiple calls
        await _handler.Handle(@event, CancellationToken.None);
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _userFactory.Received(2).AddPermissionToUser(
            user,
            applicationId.Value.ToString(),
            ResourceType.Application,
            Arg.Is<AccessType[]>(accessTypes => accessTypes.Length == 2 && accessTypes.Contains(AccessType.Read) && accessTypes.Contains(AccessType.Write)),
            createdBy,
            applicationId,
            createdOn);
    }
} 