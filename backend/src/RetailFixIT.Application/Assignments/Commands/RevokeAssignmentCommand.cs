using MediatR;
using RetailFixIT.Application.Common.Interfaces;
using RetailFixIT.Domain.Enums;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Application.Assignments.Commands;

public record RevokeAssignmentCommand(Guid JobId, Guid AssignmentId) : IRequest;

public class RevokeAssignmentCommandHandler : IRequestHandler<RevokeAssignmentCommand>
{
    private readonly IJobRepository _jobs;
    private readonly IVendorRepository _vendors;
    private readonly IAssignmentRepository _assignments;
    private readonly ICurrentUserService _user;
    private readonly IAuditService _audit;

    public RevokeAssignmentCommandHandler(
        IJobRepository jobs, IVendorRepository vendors, IAssignmentRepository assignments,
        ICurrentUserService user, IAuditService audit)
    {
        _jobs = jobs; _vendors = vendors; _assignments = assignments;
        _user = user; _audit = audit;
    }

    public async Task Handle(RevokeAssignmentCommand request, CancellationToken ct)
    {
        var assignment = await _assignments.GetByIdAsync(request.AssignmentId, ct)
            ?? throw new KeyNotFoundException($"Assignment {request.AssignmentId} not found");

        if (assignment.JobId != request.JobId)
            throw new InvalidOperationException("Assignment does not belong to this job");

        if (assignment.Status != AssignmentStatus.Active)
            throw new InvalidOperationException("Only active assignments can be revoked");

        assignment.Status = AssignmentStatus.Revoked;
        assignment.RevokedAt = DateTime.UtcNow;
        await _assignments.UpdateAsync(assignment, ct);

        // Return capacity to the vendor
        var vendor = await _vendors.GetByIdAsync(assignment.VendorId, ct);
        if (vendor is not null && vendor.CurrentCapacity > 0)
        {
            vendor.CurrentCapacity--;
            await _vendors.UpdateAsync(vendor, ct);
        }

        // Reset job status back to InReview and clear assigned vendor name
        var job = await _jobs.GetByIdAsync(request.JobId, ct);
        if (job is not null)
        {
            job.Status = JobStatus.InReview;
            job.AssignedVendorName = null;
            await _jobs.UpdateAsync(job, ct);
        }

        await _audit.LogAsync("Assignment", assignment.Id.ToString(), "Revoked",
            newValues: new { assignment.JobId, assignment.VendorId, RevokedBy = _user.UserId }, ct: ct);
    }
}
