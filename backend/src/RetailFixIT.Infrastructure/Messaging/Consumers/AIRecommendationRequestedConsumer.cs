using System.Text.Json;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using RetailFixIT.Domain.Enums;
using RetailFixIT.Domain.Events;
using RetailFixIT.Infrastructure.AI;
using RetailFixIT.Infrastructure.Persistence;

namespace RetailFixIT.Infrastructure.Messaging.Consumers;

public class AIRecommendationRequestedConsumer : IConsumer<AIRecommendationRequestedEvent>
{
    private readonly AppDbContext _db;
    private readonly IAIProvider _aiProvider;
    private readonly IPublishEndpoint _publisher;
    private readonly ILogger<AIRecommendationRequestedConsumer> _logger;

    public AIRecommendationRequestedConsumer(
        AppDbContext db, IAIProvider aiProvider, IPublishEndpoint publisher,
        ILogger<AIRecommendationRequestedConsumer> logger)
    {
        _db = db;
        _aiProvider = aiProvider;
        _publisher = publisher;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<AIRecommendationRequestedEvent> context)
    {
        var msg = context.Message;
        _logger.LogInformation("Processing AI recommendation request {RecId} for job {JobId}", msg.RecommendationId, msg.JobId);

        // Retry finding the recommendation — it may not have replicated yet in Cosmos DB
        var recommendation = await FindWithRetryAsync(msg.RecommendationId, context.CancellationToken);

        if (recommendation == null)
        {
            _logger.LogWarning("Recommendation {RecId} not found after retries — skipping", msg.RecommendationId);
            return;
        }

        // Fetch job with full details
        var job = await _db.Jobs.IgnoreQueryFilters()
            .FirstOrDefaultAsync(j => j.Id == msg.JobId, context.CancellationToken);

        // Fetch available vendors for this tenant
        var vendors = await _db.Vendors.IgnoreQueryFilters()
            .Where(v => v.TenantId == msg.TenantId && v.IsActive && v.CurrentCapacity < v.CapacityLimit)
            .Take(20)
            .ToListAsync(context.CancellationToken);

        var aiContext = new AIJobContext(
            msg.JobId,
            job?.Title ?? msg.JobTitle,
            job?.Description ?? msg.JobDescription,
            job?.ServiceType ?? msg.ServiceType,
            job?.ServiceAddress ?? msg.ServiceAddress,
            vendors.Select(v => new VendorContext(
                v.Id, v.Name, v.ServiceArea, v.Specializations,
                v.Rating, v.CapacityLimit - v.CurrentCapacity)).ToList());

        var result = await _aiProvider.GenerateRecommendationAsync(aiContext, context.CancellationToken);

        recommendation.Status = result.Success ? AIRecommendationStatus.Completed : AIRecommendationStatus.Failed;
        recommendation.RecommendedVendorIds = result.RecommendedVendorIds != null
            ? JsonSerializer.Serialize(result.RecommendedVendorIds) : null;
        recommendation.Reasoning = result.Reasoning;
        recommendation.JobSummary = result.JobSummary;
        recommendation.AIProvider = result.ProviderName;
        recommendation.ModelVersion = result.ModelVersion;
        recommendation.LatencyMs = result.LatencyMs;
        recommendation.ErrorMessage = result.ErrorMessage;
        recommendation.CompletedAt = DateTime.UtcNow;
        recommendation.PromptSummary = $"Job: {msg.JobTitle}, Type: {msg.ServiceType}, Vendors evaluated: {vendors.Count}";

        try
        {
            await _db.SaveChangesAsync(context.CancellationToken);
            _logger.LogInformation("AI recommendation {RecId} saved. Success={Success}, Status={Status}",
                msg.RecommendationId, result.Success, recommendation.Status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save AI recommendation {RecId} to database", msg.RecommendationId);
            // Re-throw so MassTransit can retry the message
            throw;
        }

        // Publish notification — wrap in try-catch so a Service Bus hiccup doesn't roll back the DB save
        try
        {
            using var pubCts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await _publisher.Publish(new AIRecommendationGeneratedEvent(
                recommendation.Id, msg.JobId, msg.TenantId,
                result.Success, result.Reasoning, result.JobSummary,
                result.RecommendedVendorIds, recommendation.CompletedAt!.Value),
                pubCts.Token);
        }
        catch (Exception ex)
        {
            // DB is already updated — this only affects SignalR push, not data integrity
            _logger.LogWarning(ex, "Failed to publish AIRecommendationGeneratedEvent for {RecId} — data is saved", msg.RecommendationId);
        }

        _logger.LogInformation("AI recommendation {RecId} done. Success={Success}", msg.RecommendationId, result.Success);
    }

    private async Task<RetailFixIT.Domain.Entities.AIRecommendation?> FindWithRetryAsync(
        Guid recommendationId, CancellationToken ct)
    {
        for (int i = 0; i < 5; i++)
        {
            var rec = await _db.AIRecommendations.IgnoreQueryFilters()
                .FirstOrDefaultAsync(r => r.Id == recommendationId, ct);
            if (rec != null) return rec;
            if (i < 4) await Task.Delay(500, ct);
        }
        return null;
    }
}
