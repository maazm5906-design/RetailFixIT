using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.API.Controllers;

[ApiController]
[Route("api/v1/audit-logs")]
[Authorize(Roles = "Admin")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogRepository _repo;

    public AuditLogsController(IAuditLogRepository repo) => _repo = repo;

    [HttpGet]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 100,
        [FromQuery] string? entityName = null,
        [FromQuery] string? entityId = null,
        [FromQuery] string? action = null,
        [FromQuery] DateTime? from = null,
        [FromQuery] DateTime? to = null,
        CancellationToken ct = default)
    {
        var (items, total) = await _repo.GetPagedAsync(page, pageSize, entityName, entityId, action, from, to, ct);
        return Ok(new
        {
            items = items.Select(a => new
            {
                a.Id,
                a.TenantId,
                a.EntityName,
                a.EntityId,
                a.Action,
                a.ChangedByUserId,
                a.ChangedByEmail,
                a.OldValues,
                a.NewValues,
                a.CorrelationId,
                a.OccurredAt
            }),
            totalCount = total,
            page,
            pageSize
        });
    }
}
