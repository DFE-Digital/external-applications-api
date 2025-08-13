using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace DfE.ExternalApplications.Api.Hubs;

[Authorize(Policy = "Cookies.CanReadNotifications")]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var userEmail = Context.User?.FindFirstValue(ClaimTypes.Email);
        
        if (!string.IsNullOrEmpty(userEmail))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userEmail}");
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userEmail = Context.User?.FindFirstValue(ClaimTypes.Email);
        
        if (!string.IsNullOrEmpty(userEmail))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userEmail}");
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}
