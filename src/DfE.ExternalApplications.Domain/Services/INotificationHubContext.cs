using GovUK.Dfe.CoreLibs.Contracts.ExternalApplications.Models.Response;

namespace DfE.ExternalApplications.Domain.Services;

/// <summary>
/// Abstraction for SignalR hub context to maintain DDD layer separation
/// </summary>
public interface INotificationHubContext
{
    /// <summary>
    /// Sends a message to a specific group
    /// </summary>
    /// <param name="groupName">The group name</param>
    /// <param name="method">The method name to call</param>
    /// <param name="args">The arguments to pass</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task SendToGroupAsync(string groupName, string method, object?[] args, CancellationToken cancellationToken = default);
}
