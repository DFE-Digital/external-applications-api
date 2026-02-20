using GovUK.Dfe.CoreLibs.Testing.AutoFixture.Attributes;
using DfE.ExternalApplications.Application.Users.EventHandlers;
using DfE.ExternalApplications.Domain.Entities;
using DfE.ExternalApplications.Domain.Events;
using DfE.ExternalApplications.Tests.Common.Customizations.Entities;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.EventHandlers;

public class UserCreatedEventHandlerTests
{
    private readonly ILogger<UserCreatedEventHandler> _logger;
    private readonly UserCreatedEventHandler _handler;

    public UserCreatedEventHandlerTests()
    {
        _logger = Substitute.For<ILogger<UserCreatedEventHandler>>();
        _handler = new UserCreatedEventHandler(_logger);
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldLogInformation_WhenUserCreated(User user)
    {
        // Arrange
        var createdOn = DateTime.UtcNow;
        var @event = new UserCreatedEvent(user, createdOn);

        // Act
        await _handler.Handle(@event, CancellationToken.None);

        // Assert
        _logger.Received(1).Log(
            LogLevel.Information,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("User created")),
            null,
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Theory]
    [CustomAutoData(typeof(UserCustomization))]
    public async Task Handle_ShouldNotThrow(User user)
    {
        // Arrange
        var @event = new UserCreatedEvent(user, DateTime.UtcNow);

        // Act & Assert
        var exception = await Record.ExceptionAsync(() => _handler.Handle(@event, CancellationToken.None));
        Assert.Null(exception);
    }
}
