namespace RetailFixIT.Infrastructure.Realtime;

public interface IRealtimeNotificationService
{
    Task NotifyTenantAsync(Guid tenantId, string eventName, object payload, CancellationToken ct = default);
    Task NotifyJobAsync(Guid jobId, Guid tenantId, string eventName, object payload, CancellationToken ct = default);
}
