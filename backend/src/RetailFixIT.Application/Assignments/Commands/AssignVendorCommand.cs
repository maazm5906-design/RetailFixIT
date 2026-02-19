using FluentValidation;
using MassTransit;
using MediatR;
using RetailFixIT.Application.Assignments.DTOs;
using RetailFixIT.Domain.Entities;
using RetailFixIT.Domain.Enums;
using RetailFixIT.Domain.Events;
using RetailFixIT.Application.Common.Interfaces;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Application.Assignments.Commands;

public record AssignVendorCommand(Guid JobId, AssignVendorRequest Request) : IRequest<AssignmentDto>;

public class AssignVendorCommandValidator : AbstractValidator<AssignVendorCommand>
{
    public AssignVendorCommandValidator()
    {
        RuleFor(x => x.JobId).NotEmpty();
        RuleFor(x => x.Request.VendorId).NotEmpty();
    }
}

public class AssignVendorCommandHandler : IRequestHandler<AssignVendorCommand, AssignmentDto>
{
    private readonly IJobRepository _jobs;
    private readonly IVendorRepository _vendors;
    private readonly IAssignmentRepository _assignments;
    private readonly IPublishEndpoint _publisher;
    private readonly ICurrentUserService _user;
    private readonly ICurrentTenantService _tenant;
    private readonly IAuditService _audit;

    public AssignVendorCommandHandler(
        IJobRepository jobs, IVendorRepository vendors, IAssignmentRepository assignments,
        IPublishEndpoint publisher, ICurrentUserService user, ICurrentTenantService tenant, IAuditService audit)
    {
        _jobs = jobs; _vendors = vendors; _assignments = assignments;
        _publisher = publisher; _user = user; _tenant = tenant; _audit = audit;
    }

    public async Task<AssignmentDto> Handle(AssignVendorCommand request, CancellationToken ct)
    {
        var job = await _jobs.GetByIdAsync(request.JobId, ct)
            ?? throw new KeyNotFoundException($"Job {request.JobId} not found");

        var vendor = await _vendors.GetByIdAsync(request.Request.VendorId, ct)
            ?? throw new KeyNotFoundException($"Vendor {request.Request.VendorId} not found");

        if (!vendor.IsActive)
            throw new InvalidOperationException("Cannot assign an inactive vendor");
        if (vendor.CurrentCapacity >= vendor.CapacityLimit)
            throw new InvalidOperationException("Vendor is at full capacity");

        // Revoke any existing active assignments before creating the new one
        var existing = await _assignments.GetByJobIdAsync(job.Id, ct);
        foreach (var active in existing.Where(a => a.Status == AssignmentStatus.Active))
        {
            active.Status = AssignmentStatus.Revoked;
            active.RevokedAt = DateTime.UtcNow;
            await _assignments.UpdateAsync(active, ct);

            // Return capacity to the previously assigned vendor
            var prevVendor = await _vendors.GetByIdAsync(active.VendorId, ct);
            if (prevVendor is not null && prevVendor.CurrentCapacity > 0)
            {
                prevVendor.CurrentCapacity--;
                await _vendors.UpdateAsync(prevVendor, ct);
            }
        }

        var assignment = new Assignment
        {
            TenantId = _tenant.TenantId,
            JobId = job.Id,
            VendorId = vendor.Id,
            VendorName = vendor.Name,
            AssignedByUserId = _user.UserId,
            Status = AssignmentStatus.Active,
            Notes = request.Request.Notes,
            AssignedAt = DateTime.UtcNow
        };

        await _assignments.AddAsync(assignment, ct);

        job.Status = JobStatus.Assigned;
        job.AssignedVendorName = vendor.Name;
        vendor.CurrentCapacity++;
        await _jobs.UpdateAsync(job, ct);
        await _vendors.UpdateAsync(vendor, ct);

        _ = PublishSafe(_publisher, new JobAssignedEvent(
            job.Id, job.TenantId, assignment.Id,
            vendor.Id, vendor.Name, _user.UserId, assignment.AssignedAt));

        await _audit.LogAsync("Assignment", assignment.Id.ToString(), "Created",
            newValues: new { assignment.JobId, assignment.VendorId, VendorName = vendor.Name }, ct: ct);

        return new AssignmentDto
        {
            Id = assignment.Id, JobId = job.Id, VendorId = vendor.Id,
            VendorName = vendor.Name, AssignedByUserId = assignment.AssignedByUserId,
            Status = assignment.Status, Notes = assignment.Notes, AssignedAt = assignment.AssignedAt
        };
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
}
