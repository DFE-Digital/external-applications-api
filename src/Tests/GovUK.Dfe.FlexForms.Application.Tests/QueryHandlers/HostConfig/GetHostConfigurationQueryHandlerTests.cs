using GovUK.Dfe.FlexForms.Application.HostConfig.Queries;
using GovUK.Dfe.FlexForms.Domain.Tenancy;
using NSubstitute;

namespace GovUK.Dfe.FlexForms.Application.Tests.QueryHandlers.HostConfig;

public class GetHostConfigurationQueryHandlerTests
{
    [Fact]
    public async Task Handle_ShouldReturnHostConfiguration_WhenTargetIsWeb()
    {
        var reader = Substitute.For<IHostConfigurationReader>();
        reader.GetConfiguration("Web").Returns(new HostConfigurationSnapshot(
            "Web",
            DateTime.UtcNow,
            new Dictionary<string, string?> { ["ApplicationInsights:ConnectionString"] = "test" }));

        var handler = new GetHostConfigurationQueryHandler(reader);

        var result = await handler.Handle(new GetHostConfigurationQuery("Web"), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Web", result.Value!.Target);
        Assert.Equal("test", result.Value.Configuration["ApplicationInsights:ConnectionString"]);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenTargetIsInvalid()
    {
        var reader = Substitute.For<IHostConfigurationReader>();
        var handler = new GetHostConfigurationQueryHandler(reader);

        var result = await handler.Handle(new GetHostConfigurationQuery("Shared"), CancellationToken.None);

        Assert.False(result.IsSuccess);
        reader.DidNotReceive().GetConfiguration(Arg.Any<string>());
    }
}
