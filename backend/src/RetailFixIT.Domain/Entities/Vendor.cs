using RetailFixIT.Domain.Common;

namespace RetailFixIT.Domain.Entities;

public class Vendor : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ServiceArea { get; set; }
    public string? Specializations { get; set; } // JSON array of tags
    public int CapacityLimit { get; set; } = 10;
    public int CurrentCapacity { get; set; } = 0;
    public decimal? Rating { get; set; }
    public bool IsActive { get; set; } = true;

    public Tenant Tenant { get; set; } = null!;
    public ICollection<Assignment> Assignments { get; set; } = new List<Assignment>();
}
