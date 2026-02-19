namespace RetailFixIT.Application.Vendors.DTOs;

public class VendorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ServiceArea { get; set; }
    public string? Specializations { get; set; }
    public int CapacityLimit { get; set; }
    public int CurrentCapacity { get; set; }
    public int AvailableCapacity => CapacityLimit - CurrentCapacity;
    public decimal? Rating { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateVendorRequest
{
    public string Name { get; set; } = string.Empty;
    public string ContactEmail { get; set; } = string.Empty;
    public string? ContactPhone { get; set; }
    public string? ServiceArea { get; set; }
    public string? Specializations { get; set; }
    public int CapacityLimit { get; set; } = 10;
}
