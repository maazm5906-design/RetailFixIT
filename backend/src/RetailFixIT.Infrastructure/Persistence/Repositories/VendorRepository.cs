using Microsoft.EntityFrameworkCore;
using RetailFixIT.Domain.Entities;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Infrastructure.Persistence.Repositories;

public class VendorRepository : IVendorRepository
{
    private readonly AppDbContext _db;

    public VendorRepository(AppDbContext db) => _db = db;

    public async Task<Vendor?> GetByIdAsync(Guid id, CancellationToken ct = default)
        => await _db.Vendors.FirstOrDefaultAsync(v => v.Id == id, ct);

    public async Task<(IEnumerable<Vendor> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        bool? isActive = null,
        bool? hasCapacity = null,
        string? serviceType = null,
        CancellationToken ct = default)
    {
        var query = _db.Vendors.AsQueryable();

        if (isActive.HasValue) query = query.Where(v => v.IsActive == isActive.Value);
        if (hasCapacity == true) query = query.Where(v => v.CurrentCapacity < v.CapacityLimit);
        if (!string.IsNullOrEmpty(serviceType))
            query = query.Where(v => v.Specializations != null && v.Specializations.Contains(serviceType));

        var total = await query.CountAsync(ct);
        var items = await query.OrderBy(v => v.Name).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }

    public async Task<IEnumerable<Vendor>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default)
        => await _db.Vendors.Where(v => ids.Contains(v.Id)).ToListAsync(ct);

    public async Task<Vendor> AddAsync(Vendor vendor, CancellationToken ct = default)
    {
        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync(ct);
        return vendor;
    }

    public async Task UpdateAsync(Vendor vendor, CancellationToken ct = default)
    {
        _db.Vendors.Update(vendor);
        await _db.SaveChangesAsync(ct);
    }
}
