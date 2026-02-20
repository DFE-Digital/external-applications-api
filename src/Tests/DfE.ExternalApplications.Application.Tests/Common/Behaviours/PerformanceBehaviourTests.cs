using DfE.ExternalApplications.Application.Common.Behaviours;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Common.Behaviours;

public class PerformanceBehaviourTests
{
    public record TestRequest(string Value) : IRequest<string>;

    [Fact]
    public async Task Handle_ShouldReturnResponse_WhenRequestCompletesQuickly()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestRequest>>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var behaviour = new PerformanceBehaviour<TestRequest, string>(logger, httpContextAccessor);

        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("success");

        // Act
        var result = await behaviour.Handle(new TestRequest("test"), next, CancellationToken.None);

        // Assert
        Assert.Equal("success", result);
        await next.Received(1).Invoke();
    }

    [Fact]
    public async Task Handle_ShouldCallNext_AndReturnResult()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestRequest>>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var behaviour = new PerformanceBehaviour<TestRequest, string>(logger, httpContextAccessor);

        var expectedResult = "expected-result";
        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns(expectedResult);

        // Act
        var result = await behaviour.Handle(new TestRequest("val"), next, CancellationToken.None);

        // Assert
        Assert.Equal(expectedResult, result);
    }

    [Fact]
    public async Task Handle_ShouldNotLog_WhenRequestCompletesWithin1000ms()
    {
        // Arrange
        var logger = Substitute.For<ILogger<TestRequest>>();
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var behaviour = new PerformanceBehaviour<TestRequest, string>(logger, httpContextAccessor);

        var next = Substitute.For<RequestHandlerDelegate<string>>();
        next().Returns("fast");

        // Act
        await behaviour.Handle(new TestRequest("test"), next, CancellationToken.None);

        // Assert
        logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}
