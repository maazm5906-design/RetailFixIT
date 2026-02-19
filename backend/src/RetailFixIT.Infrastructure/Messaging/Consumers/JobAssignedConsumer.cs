using MassTransit;
using Microsoft.Extensions.Logging;
using RetailFixIT.Domain.Entities;
using RetailFixIT.Domain.Events;
using RetailFixIT.Infrastructure.Persistence;
using RetailFixIT.Infrastructure.Realtime;

namespace RetailFixIT.Infrastructure.Messaging.Consumers;

public class JobAssignedConsumer : IConsumer<JobAssignedEvent>
{
    private readonly AppDbContext _db;
    private readonly IRealtimeNotificationService _realtime;
    private readonly ILogger<JobAssignedConsumer> _logger;

    public JobAssignedConsumer(AppDbContext db, IRealtimeNotificationService realtime, ILogger<JobAssignedConsumer> logger)
    {
        _db = db;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<JobAssignedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Broadcasting assignment for job {JobId} to vendor {VendorId}", msg.JobId, msg.VendorId);

        await _realtime.NotifyTenantAsync(msg.TenantId, "JobAssigned", new
        {
            jobId = msg.JobId,
            vendorId = msg.VendorId,
            vendorName = msg.VendorName,
            assignedByUserId = msg.AssignedByUserId,
            assignedAt = msg.AssignedAt
        }, context.CancellationToken);

        _db.AuditLogs.Add(new AuditLog
        {
            TenantId = msg.TenantId,
            EntityName = "Job",
            EntityId = msg.JobId.ToString(),
            Action = "Assigned",
            ChangedByUserId = msg.AssignedByUserId,
            ChangedByEmail = msg.AssignedByUserId,
            NewValues = System.Text.Json.JsonSerializer.Serialize(new
            {
                assignmentId = msg.AssignmentId,
                vendorId = msg.VendorId,
                vendorName = msg.VendorName
            }),
            OccurredAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
