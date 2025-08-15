using DfE.ExternalApplications.Domain.Services;
using Microsoft.AspNetCore.SignalR;

namespace DfE.ExternalApplications.Api.Services;

/// <summary>
/// Implementation of INotificationHubContext that wraps SignalR hub context
/// </summary>
public class NotificationHubContext : INotificationHubContext
{
    private readonly IHubContext<Hubs.NotificationHub> _hubContext;

    public NotificationHubContext(IHubContext<Hubs.NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task SendToGroupAsync(string groupName, string method, object?[] args, CancellationToken cancellationToken = default)
    {
        await _hubContext.Clients.Group(groupName).SendAsync(method, args, cancellationToken);
    }
}
