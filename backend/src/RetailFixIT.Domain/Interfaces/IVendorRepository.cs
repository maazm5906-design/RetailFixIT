using RetailFixIT.Domain.Entities;

namespace RetailFixIT.Domain.Interfaces;

public interface IVendorRepository
{
    Task<Vendor?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<(IEnumerable<Vendor> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        bool? isActive = null,
        bool? hasCapacity = null,
        string? serviceType = null,
        CancellationToken ct = default);
    Task<IEnumerable<Vendor>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);
    Task<Vendor> AddAsync(Vendor vendor, CancellationToken ct = default);
    Task UpdateAsync(Vendor vendor, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
