using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Interfaces;
using GovUK.Dfe.CoreLibs.Messaging.MassTransit.Models;

namespace DfE.ExternalApplications.Tests.Common.Helpers;

/// <summary>
/// Mock event publisher that simulates publishing events without actually sending them
/// </summary>
public class MockEventPublisher : IEventPublisher
{
    public Task PublishAsync<T>(T message, AzureServiceBusMessageProperties? messageProperties = null, CancellationToken cancellationToken = default) where T : class
    {
        // Simulate successful publish without actually sending to Azure Service Bus
        return Task.CompletedTask;
    }

    public Task PublishAsync<T>(T @event, CancellationToken cancellationToken = default) where T : class
    {
        throw new NotImplementedException();
    }
}

