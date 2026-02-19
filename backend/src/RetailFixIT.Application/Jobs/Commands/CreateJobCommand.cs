using FluentValidation;
using MassTransit;
using MediatR;
using RetailFixIT.Application.Jobs.DTOs;
using RetailFixIT.Domain.Entities;
using RetailFixIT.Domain.Enums;
using RetailFixIT.Domain.Events;
using RetailFixIT.Application.Common.Interfaces;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Application.Jobs.Commands;

public record CreateJobCommand(CreateJobRequest Request) : IRequest<JobDto>;

public class CreateJobCommandValidator : AbstractValidator<CreateJobCommand>
{
    public CreateJobCommandValidator()
    {
        RuleFor(x => x.Request.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Request.CustomerName).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Request.ServiceAddress).NotEmpty().MaximumLength(500);
        RuleFor(x => x.Request.ServiceType).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Request.CustomerEmail).EmailAddress().When(x => !string.IsNullOrEmpty(x.Request.CustomerEmail));
    }
}

public class CreateJobCommandHandler : IRequestHandler<CreateJobCommand, JobDto>
{
    private readonly IJobRepository _jobs;
    private readonly IPublishEndpoint _publisher;
    private readonly ICurrentUserService _user;
    private readonly ICurrentTenantService _tenant;
    private readonly IAuditService _audit;

    public CreateJobCommandHandler(
        IJobRepository jobs,
        IPublishEndpoint publisher,
        ICurrentUserService user,
        ICurrentTenantService tenant,
        IAuditService audit)
    {
        _jobs = jobs;
        _publisher = publisher;
        _user = user;
        _tenant = tenant;
        _audit = audit;
    }

    public async Task<JobDto> Handle(CreateJobCommand request, CancellationToken ct)
    {
        var jobNumber = await _jobs.GenerateJobNumberAsync(ct);
        var job = new Job
        {
            TenantId = _tenant.TenantId,
            JobNumber = jobNumber,
            Title = request.Request.Title,
            Description = request.Request.Description,
            CustomerName = request.Request.CustomerName,
            CustomerEmail = request.Request.CustomerEmail,
            CustomerPhone = request.Request.CustomerPhone,
            ServiceAddress = request.Request.ServiceAddress,
            ServiceType = request.Request.ServiceType,
            Priority = request.Request.Priority,
            ScheduledAt = request.Request.ScheduledAt,
            Status = JobStatus.New,
            CreatedByUserId = _user.UserId
        };

        await _jobs.AddAsync(job, ct);

        // Publish fire-and-forget with own timeout so a slow Service Bus never blocks the HTTP response.
        // Data is already persisted — event drives AI/SignalR but is not required for the 201.
        _ = PublishSafe(_publisher, new JobCreatedEvent(
            job.Id, job.TenantId, job.JobNumber, job.Title,
            job.ServiceType, job.CreatedByUserId, job.CreatedAt));

        await _audit.LogAsync("Job", job.Id.ToString(), "Created",
            newValues: new { job.JobNumber, job.Title, Status = job.Status.ToString() }, ct: ct);

        return MapToDto(job);
    }

    private static async Task PublishSafe<T>(IPublishEndpoint publisher, T message) where T : class
    {
        try
        {
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            await publisher.Publish(message, cts.Token);
        }
        catch { /* Service Bus unavailable — data already saved, event is best-effort */ }
    }

    private static JobDto MapToDto(Job job) => new()
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
        CreatedAt = job.CreatedAt,
        CreatedByUserId = job.CreatedByUserId
    };
}
