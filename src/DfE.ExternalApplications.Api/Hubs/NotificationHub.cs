using DfE.ExternalApplications.Domain.Tenancy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace DfE.ExternalApplications.Api.Hubs;

[Authorize(Policy = "Cookies.CanReadNotifications")]
public class NotificationHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        var httpContext = Context.GetHttpContext();
        var tenantAccessor = httpContext?.RequestServices.GetService<ITenantContextAccessor>();
        var tenantId = tenantAccessor?.CurrentTenant?.Id;
        
        var userEmail = Context.User?.FindFirstValue(ClaimTypes.Email);
        
        if (!string.IsNullOrEmpty(userEmail) && tenantId.HasValue)
        {
            // Include tenant ID in group name for tenant isolation
            await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}:user:{userEmail}");
        }
        else if (!string.IsNullOrEmpty(userEmail))
        {
            // Fallback for cases without tenant (shouldn't happen in production)
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{userEmail}");
        }
        
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var httpContext = Context.GetHttpContext();
        var tenantAccessor = httpContext?.RequestServices.GetService<ITenantContextAccessor>();
        var tenantId = tenantAccessor?.CurrentTenant?.Id;
        
        var userEmail = Context.User?.FindFirstValue(ClaimTypes.Email);
        
        if (!string.IsNullOrEmpty(userEmail) && tenantId.HasValue)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"tenant:{tenantId}:user:{userEmail}");
        }
        else if (!string.IsNullOrEmpty(userEmail))
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{userEmail}");
        }
        
        await base.OnDisconnectedAsync(exception);
    }
}
