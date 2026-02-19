using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RetailFixIT.Domain.Entities;
using RetailFixIT.Domain.Enums;
using RetailFixIT.Domain.Events;
using RetailFixIT.Infrastructure.Persistence;

namespace RetailFixIT.Infrastructure.Messaging.Consumers;

public class JobCreatedConsumer : IConsumer<JobCreatedEvent>
{
    private readonly AppDbContext _db;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<JobCreatedConsumer> _logger;

    public JobCreatedConsumer(AppDbContext db, IPublishEndpoint publisher, ILogger<JobCreatedConsumer> logger)
    {
        _db = db;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<JobCreatedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing JobCreated for {JobNumber} ({JobId})", msg.JobNumber, msg.JobId);

        var job = await _db.Jobs.IgnoreQueryFilters().FirstOrDefaultAsync(j => j.Id == msg.JobId, context.CancellationToken);
        if (job == null)
        {
            _logger.LogWarning("Job {JobId} not found in consumer", msg.JobId);
            return;
        }

        if (job.Status == JobStatus.New)
        {
            job.Status = JobStatus.InReview;
            job.UpdatedAt = DateTime.UtcNow;
        }

        // Auto-create pending AI recommendation
        var recommendation = new AIRecommendation
        {
            TenantId = msg.TenantId,
            JobId = msg.JobId,
            RequestedByUserId = msg.CreatedByUserId,
            Status = AIRecommendationStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        _db.AIRecommendations.Add(recommendation);
        await _db.SaveChangesAsync(context.CancellationToken);

        await _publisher.Publish(new AIRecommendationRequestedEvent(
            recommendation.Id, msg.JobId, msg.TenantId,
            msg.Title, string.Empty, msg.ServiceType, string.Empty,
            msg.CreatedByUserId, recommendation.RequestedAt),
            context.CancellationToken);

        _logger.LogInformation("Triggered AI recommendation {RecId} for job {JobId}", recommendation.Id, msg.JobId);
    }
}
