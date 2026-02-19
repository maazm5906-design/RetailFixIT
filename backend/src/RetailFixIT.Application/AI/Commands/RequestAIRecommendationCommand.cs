using MassTransit;
using MediatR;
using RetailFixIT.Domain.Entities;
using RetailFixIT.Domain.Enums;
using RetailFixIT.Domain.Events;
using RetailFixIT.Application.Common.Interfaces;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Application.AI.Commands;

public record RequestAIRecommendationCommand(Guid JobId) : IRequest<AIRecommendationResult>;

public class AIRecommendationResult
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public AIRecommendationStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
}

public class RequestAIRecommendationCommandHandler : IRequestHandler<RequestAIRecommendationCommand, AIRecommendationResult>
{
    private readonly IJobRepository _jobs;
    private readonly IAIRecommendationRepository _recommendations;
    private readonly IPublishEndpoint _publisher;
    private readonly ICurrentUserService _user;
    private readonly ICurrentTenantService _tenant;

    public RequestAIRecommendationCommandHandler(
        IJobRepository jobs, IAIRecommendationRepository recommendations,
        IPublishEndpoint publisher, ICurrentUserService user, ICurrentTenantService tenant)
    {
        _jobs = jobs; _recommendations = recommendations;
        _publisher = publisher; _user = user; _tenant = tenant;
    }

    public async Task<AIRecommendationResult> Handle(RequestAIRecommendationCommand request, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(request.JobId, ct)
            ?? throw new KeyNotFoundException($"Job {request.JobId} not found");

        var recommendation = new AIRecommendation
        {
            TenantId = _tenant.TenantId,
            JobId = job.Id,
            RequestedByUserId = _user.UserId,
            Status = AIRecommendationStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        await _recommendations.AddAsync(recommendation, ct);

        // Publish with a short timeout. If Service Bus is unavailable, mark the
        // recommendation Failed immediately so the UI never spins indefinitely.
        try
        {
            using var pubCts = new CancellationTokenSource(TimeSpan.FromSeconds(8));
            await _publisher.Publish(new AIRecommendationRequestedEvent(
                recommendation.Id, job.Id, job.TenantId,
                job.Title, job.Description ?? string.Empty,
                job.ServiceType, job.ServiceAddress,
                _user.UserId, recommendation.RequestedAt), pubCts.Token);
        }
        catch
        {
            // Consumer will never run — mark failed now so frontend shows error state.
            recommendation.Status = AIRecommendationStatus.Failed;
            recommendation.CompletedAt = DateTime.UtcNow;
            await _recommendations.UpdateAsync(recommendation, ct);
        }

        return new AIRecommendationResult
        {
            Id = recommendation.Id,
            JobId = job.Id,
            Status = recommendation.Status,
            RequestedAt = recommendation.RequestedAt
        };
    }
}
