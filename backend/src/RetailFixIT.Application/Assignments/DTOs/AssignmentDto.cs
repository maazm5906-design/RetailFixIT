using RetailFixIT.Domain.Enums;

namespace RetailFixIT.Application.Assignments.DTOs;

public class AssignmentDto
{
    public Guid Id { get; set; }
    public Guid JobId { get; set; }
    public Guid VendorId { get; set; }
    public string VendorName { get; set; } = string.Empty;
    public string AssignedByUserId { get; set; } = string.Empty;
    public AssignmentStatus Status { get; set; }
    public string? Notes { get; set; }
    public DateTime AssignedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class AssignVendorRequest
{
    public Guid VendorId { get; set; }
    public string? Notes { get; set; }
}
