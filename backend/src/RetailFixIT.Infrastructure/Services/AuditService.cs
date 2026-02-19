using System.Text.Json;
using Microsoft.AspNetCore.Http;
using RetailFixIT.Application.Common.Interfaces;
using RetailFixIT.Domain.Entities;
using RetailFixIT.Domain.Interfaces;

namespace RetailFixIT.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly IAuditLogRepository _repo;
    private readonly ICurrentUserService _user;
    private readonly ICurrentTenantService _tenant;
    private readonly IHttpContextAccessor _http;

    public AuditService(
        IAuditLogRepository repo,
        ICurrentUserService user,
        ICurrentTenantService tenant,
        IHttpContextAccessor http)
    {
        _repo = repo;
        _user = user;
        _tenant = tenant;
        _http = http;
    }

    public async Task LogAsync(
        string entityName,
        string entityId,
        string action,
        object? oldValues = null,
        object? newValues = null,
        Guid? correlationId = null,
        CancellationToken ct = default)
    {
        var log = new AuditLog
        {
            TenantId = _tenant.TenantId,
            EntityName = entityName,
            EntityId = entityId,
            Action = action,
            ChangedByUserId = _user.UserId,
            ChangedByEmail = _user.Email,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : null,
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : null,
            IpAddress = _http.HttpContext?.Connection.RemoteIpAddress?.ToString(),
            UserAgent = _http.HttpContext?.Request.Headers.UserAgent.ToString(),
            CorrelationId = correlationId,
            OccurredAt = DateTime.UtcNow
        };

        await _repo.AddAsync(log, ct);
    }
}
