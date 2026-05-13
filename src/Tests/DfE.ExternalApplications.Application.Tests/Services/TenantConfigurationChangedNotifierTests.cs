using DfE.ExternalApplications.Infrastructure.Services;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace DfE.ExternalApplications.Application.Tests.Services;

public class TenantConfigurationChangedNotifierTests
{
    private static TenantConfigurationChangedNotifier CreateSut(out ILogger<TenantConfigurationChangedNotifier> logger)
    {
        logger = Substitute.For<ILogger<TenantConfigurationChangedNotifier>>();
        return new TenantConfigurationChangedNotifier(logger);
    }

    [Fact]
    public void Notify_FiresAllSubscribers()
    {
        var sut = CreateSut(out _);
        var calls = 0;
        sut.Changed += () => calls++;
        sut.Changed += () => calls++;

        sut.Notify();

        Assert.Equal(2, calls);
    }

    [Fact]
    public void Notify_ContinuesToOtherSubscribers_WhenOneThrows()
    {
        var sut = CreateSut(out _);
        var calls = 0;
        sut.Changed += () => throw new InvalidOperationException("boom");
        sut.Changed += () => calls++;

        sut.Notify();

        Assert.Equal(1, calls);
    }

    [Fact]
    public void Notify_NoSubscribers_DoesNotThrow()
    {
        var sut = CreateSut(out _);

        var ex = Record.Exception(() => sut.Notify());

        Assert.Null(ex);
    }
}
