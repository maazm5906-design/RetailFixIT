using FluentValidation;
using MediatR;
using RetailFixIT.Application.Jobs.DTOs;
using RetailFixIT.Application.Common.Interfaces;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Application.Jobs.Commands;

public record UpdateJobCommand(Guid JobId, UpdateJobRequest Request) : IRequest<JobDto>;

public class UpdateJobCommandValidator : AbstractValidator<UpdateJobCommand>
{
    public UpdateJobCommandValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.ServiceAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.ServiceType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.CustomerEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.Request.CustomerEmail));
    }
}

public class UpdateJobCommandHandler : IRequestHandler<UpdateJobCommand, JobDto>
{
    private readonly IJobRepository _jobs;
    private readonly IAuditService _audit;

    public UpdateJobCommandHandler(IJobRepository jobs, IAuditService audit)
    {
        _jobs = jobs;
        _audit = audit;
    }

    public async Task<JobDto> Handle(UpdateJobCommand request, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(request.JobId, ct)
            ?? throw new KeyNotFoundException($"Job {request.JobId} not found");

        var r = request.Request;
        job.Title = r.Title;
        job.Description = r.Description;
        job.CustomerName = r.CustomerName;
        job.CustomerEmail = r.CustomerEmail;
        job.CustomerPhone = r.CustomerPhone;
        job.ServiceAddress = r.ServiceAddress;
        job.ServiceType = r.ServiceType;
        job.Priority = r.Priority;
        job.ScheduledAt = r.ScheduledAt;
        job.UpdatedAt = DateTime.UtcNow;

        await _jobs.UpdateAsync(job, ct);

        await _audit.LogAsync("Job", job.Id.ToString(), "Updated",
            newValues: new { r.Title, r.Priority, r.ServiceType }, ct: ct);

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
            CreatedByUserId = job.CreatedByUserId
        };
    }
}
