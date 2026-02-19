using Microsoft.EntityFrameworkCore;
using RetailFixIT.Domain.Entities;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Infrastructure.Persistence.Repositories;

public class AuditLogRepository : IAuditLogRepository
{
    private readonly AppDbContext _db;

    public AuditLogRepository(AppDbContext db) => _db = db;

    public async Task AddAsync(AuditLog log, CancellationToken ct = default)
    {
        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync(ct);
    }

    public async Task<(IEnumerable<AuditLog> Items, int TotalCount)> GetPagedAsync(
        int page, int pageSize,
        string? entityName = null,
        string? entityId = null,
        string? action = null,
        DateTime? from = null,
        DateTime? to = null,
        CancellationToken ct = default)
    {
        var query = _db.AuditLogs.AsQueryable();

        if (!string.IsNullOrEmpty(entityName)) query = query.Where(a => a.EntityName == entityName);
        if (!string.IsNullOrEmpty(entityId)) query = query.Where(a => a.EntityId == entityId);
        if (!string.IsNullOrEmpty(action)) query = query.Where(a => a.Action == action);
        if (from.HasValue) query = query.Where(a => a.OccurredAt >= from.Value);
        if (to.HasValue) query = query.Where(a => a.OccurredAt <= to.Value);

        var total = await query.CountAsync(ct);
        var items = await query.OrderByDescending(a => a.OccurredAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return (items, total);
    }
}
