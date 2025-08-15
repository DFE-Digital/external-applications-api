using DfE.ExternalApplications.Domain.Services;

namespace DfE.ExternalApplications.Tests.Common.Mocks;

/// <summary>
/// Mock implementation of INotificationHubContext for testing
/// </summary>
public class MockNotificationHubContext : INotificationHubContext
{
    public List<(string GroupName, string Method, object?[] Args)> SentMessages { get; } = new();

    public Task SendToGroupAsync(string groupName, string method, object?[] args, CancellationToken cancellationToken = default)
    {
        SentMessages.Add((groupName, method, args));
        return Task.CompletedTask;
    }

    public void Reset()
    {
        SentMessages.Clear();
    }
}
