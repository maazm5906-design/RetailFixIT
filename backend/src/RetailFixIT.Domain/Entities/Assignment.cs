using RetailFixIT.Domain.Common;
using RetailFixIT.Domain.Enums;

namespace RetailFixIT.Domain.Entities;

public class Assignment : BaseEntity
{
    public Guid JobId { get; set; }
    public Guid VendorId { get; set; }
    public string AssignedByUserId { get; set; } = string.Empty;
    public AssignmentStatus Status { get; set; } = AssignmentStatus.Active;
    public string? Notes { get; set; }
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public string? RevokedByUserId { get; set; }
    public string VendorName { get; set; } = string.Empty; // Denormalized for Cosmos
    public DateTime? CompletedAt { get; set; }

    // Navigation properties — ignored by EF Cosmos, queried separately
    public Job Job { get; set; } = null!;
    public Vendor Vendor { get; set; } = null!;
}
