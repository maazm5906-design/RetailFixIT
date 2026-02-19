using MassTransit;
using Microsoft.Extensions.Logging;
using RetailFixIT.Domain.Entities;
using RetailFixIT.Domain.Events;
using RetailFixIT.Infrastructure.Persistence;
using RetailFixIT.Infrastructure.Realtime;

namespace RetailFixIT.Infrastructure.Messaging.Consumers;

public class AIRecommendationGeneratedConsumer : IConsumer<AIRecommendationGeneratedEvent>
{
    private readonly AppDbContext _db;
    private readonly IRealtimeNotificationService _realtime;
    private readonly ILogger<AIRecommendationGeneratedConsumer> _logger;

    public AIRecommendationGeneratedConsumer(AppDbContext db, IRealtimeNotificationService realtime,
        ILogger<AIRecommendationGeneratedConsumer> logger)
    {
        _db = db;
        _realtime = realtime;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AIRecommendationGeneratedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Broadcasting AI ready for job {JobId}", msg.JobId);

        await _realtime.NotifyJobAsync(msg.JobId, msg.TenantId, "AIRecommendationReady", new
        {
            jobId = msg.JobId,
            recommendationId = msg.RecommendationId,
            success = msg.Success,
            completedAt = msg.CompletedAt
        }, context.CancellationToken);

        _db.AuditLogs.Add(new AuditLog
        {
            TenantId = msg.TenantId,
            EntityName = "AIRecommendation",
            EntityId = msg.RecommendationId.ToString(),
            Action = msg.Success ? "Generated" : "Failed",
            ChangedByUserId = "system",
            ChangedByEmail = "system@retailfixit.internal",
            NewValues = System.Text.Json.JsonSerializer.Serialize(new { msg.Success, msg.RecommendedVendorIds }),
            OccurredAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(context.CancellationToken);
    }
}
