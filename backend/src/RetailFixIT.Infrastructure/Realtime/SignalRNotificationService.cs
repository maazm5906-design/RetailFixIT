using Microsoft.AspNetCore.SignalR;

namespace RetailFixIT.Infrastructure.Realtime;

public class SignalRNotificationService : IRealtimeNotificationService
{
    private readonly IHubContext<JobHub> _hubContext;

    public SignalRNotificationService(IHubContext<JobHub> hubContext)
    {
        _hubContext = hubContext;
    }

    public async Task NotifyTenantAsync(Guid tenantId, string eventName, object payload, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"tenant:{tenantId}").SendAsync(eventName, payload, ct);
    }

    public async Task NotifyJobAsync(Guid jobId, Guid tenantId, string eventName, object payload, CancellationToken ct = default)
    {
        await _hubContext.Clients.Group($"job:{jobId}").SendAsync(eventName, payload, ct);
        await _hubContext.Clients.Group($"tenant:{tenantId}").SendAsync(eventName, payload, ct);
    }
}

public class JobHub : Hub
{
    public async Task JoinTenantGroup(string tenantId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"tenant:{tenantId}");
    }

    public async Task JoinJobGroup(string jobId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"job:{jobId}");
    }

    public async Task LeaveJobGroup(string jobId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"job:{jobId}");
    }
}
