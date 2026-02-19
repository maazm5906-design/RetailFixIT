using MediatR;
using RetailFixIT.Application.Jobs.DTOs;
using RetailFixIT.Domain.Enums;
using RetailFixIT.Application.Common.Interfaces;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Application.Jobs.Commands;

public record UpdateJobStatusCommand(Guid JobId, JobStatus NewStatus) : IRequest<JobDto>;

public class UpdateJobStatusCommandHandler : IRequestHandler<UpdateJobStatusCommand, JobDto>
{
    private readonly IJobRepository _jobs;
    private readonly IAuditService _audit;

    public UpdateJobStatusCommandHandler(IJobRepository jobs, IAuditService audit)
    {
        _jobs = jobs;
        _audit = audit;
    }

    public async Task<JobDto> Handle(UpdateJobStatusCommand request, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(request.JobId, ct)
            ?? throw new KeyNotFoundException($"Job {request.JobId} not found");

        var oldStatus = job.Status;
        job.Status = request.NewStatus;

        if (request.NewStatus == JobStatus.Completed) job.CompletedAt = DateTime.UtcNow;
        if (request.NewStatus == JobStatus.Cancelled) job.CancelledAt = DateTime.UtcNow;

        await _jobs.UpdateAsync(job, ct);
        await _audit.LogAsync("Job", job.Id.ToString(), "StatusChanged",
            oldValues: new { Status = oldStatus.ToString() },
            newValues: new { Status = request.NewStatus.ToString() }, ct: ct);

        return new JobDto
        {
            Id = job.Id, JobNumber = job.JobNumber, Title = job.Title,
            Description = job.Description, CustomerName = job.CustomerName,
            ServiceAddress = job.ServiceAddress, ServiceType = job.ServiceType,
            Status = job.Status, Priority = job.Priority,
            ScheduledAt = job.ScheduledAt, CompletedAt = job.CompletedAt,
            CreatedAt = job.CreatedAt, UpdatedAt = job.UpdatedAt,
            CreatedByUserId = job.CreatedByUserId
        };
    }
}
