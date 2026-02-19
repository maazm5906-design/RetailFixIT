using MediatR;
using RetailFixIT.Application.Jobs.DTOs;
using RetailFixIT.Domain.Enums;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Application.Jobs.Queries;

public record GetJobByIdQuery(Guid JobId) : IRequest<JobDto>;

public class GetJobByIdQueryHandler : IRequestHandler<GetJobByIdQuery, JobDto>
{
    private readonly IJobRepository _jobs;
    private readonly IAssignmentRepository _assignments;
    private readonly IAIRecommendationRepository _aiRecs;

    public GetJobByIdQueryHandler(
        IJobRepository jobs,
        IAssignmentRepository assignments,
        IAIRecommendationRepository aiRecs)
    {
        _jobs = jobs;
        _assignments = assignments;
        _aiRecs = aiRecs;
    }

    public async Task<JobDto> Handle(GetJobByIdQuery request, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(request.JobId, ct)
            ?? throw new KeyNotFoundException($"Job {request.JobId} not found");

        // Cosmos DB: navigate collections via separate repository calls (no cross-container Include)
        var assignments = await _assignments.GetByJobIdAsync(request.JobId, ct);
        var recs = await _aiRecs.GetByJobIdAsync(request.JobId, ct);

        return new JobDto
        {
            Id = job.Id,
            JobNumber = job.JobNumber,
            Title = job.Title,
            Description = job.Description,
            CustomerName = job.CustomerName,
            CustomerEmail = job.CustomerEmail,
            CustomerPhone = job.CustomerPhone,
            ServiceAddress = job.ServiceAddress,
            ServiceType = job.ServiceType,
            Status = job.Status,
            Priority = job.Priority,
            ScheduledAt = job.ScheduledAt,
            CompletedAt = job.CompletedAt,
            CreatedAt = job.CreatedAt,
            UpdatedAt = job.UpdatedAt,
            CreatedByUserId = job.CreatedByUserId,
            HasAIRecommendation = recs.Any(r => r.Status == AIRecommendationStatus.Completed),
            ActiveAssignment = assignments
                .Where(a => a.Status == AssignmentStatus.Active)
                .OrderByDescending(a => a.AssignedAt)
                .Select(a => new AssignmentSummaryDto
                {
                    Id = a.Id,
                    VendorId = a.VendorId,
                    VendorName = a.VendorName,
                    Status = a.Status.ToString(),
                    AssignedAt = a.AssignedAt
                }).FirstOrDefault()
        };
    }
}
