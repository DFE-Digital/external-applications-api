using DfE.ExternalApplications.Application.Common.Behaviours;
using MediatR;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Common.Behaviours;

public class UnhandledExceptionBehaviourTests
{
    public record TestRequest(string Value) : IRequest<string>;

    [Fact]
    public async Task Handle_ShouldReturnResponse_WhenNoException()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestRequest>>();
        var behaviour = new UnhandledExceptionBehaviour<TestRequest, string>(logger);

        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("success");

        // Act
        var result = await behaviour.Handle(new TestRequest("test"), next, CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        await next.Received(1).Invoke();
    }

    [Fact]
    public async Task Handle_ShouldLogError_AndRethrow_WhenExceptionOccurs()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestRequest>>();
        var behaviour = new UnhandledExceptionBehaviour<TestRequest, string>(logger);

        var next = Substitute.For<RequestHandlerDelegate<string>>();
        var expectedException = new InvalidOperationException("Something went wrong");
        next().Returns(Task.FromException<string>(expectedException));

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            behaviour.Handle(new TestRequest("test"), next, CancellationToken.None));

        Assert.Equal("Something went wrong", exception.Message);

        logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("Unhandled Exception")),
            Arg.Is<Exception>(ex => ex == expectedException),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_ShouldNotLogAnything_WhenNextSucceeds()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestRequest>>();
        var behaviour = new UnhandledExceptionBehaviour<TestRequest, string>(logger);

        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("ok");

        // Act
        await behaviour.Handle(new TestRequest("test"), next, CancellationToken.None);

        // Assert
        logger.DidNotReceive().Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_ShouldPreserveExceptionType()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestRequest>>();
        var behaviour = new UnhandledExceptionBehaviour<TestRequest, string>(logger);

        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns(Task.FromException<string>(new ArgumentNullException("param")));

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() =>
            behaviour.Handle(new TestRequest("test"), next, CancellationToken.None));
    }
}
