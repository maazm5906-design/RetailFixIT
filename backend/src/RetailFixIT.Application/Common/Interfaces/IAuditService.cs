namespace RetailFixIT.Application.Common.Interfaces;

/// <summary>
/// Writes immutable audit log entries.
/// Implemented in Infrastructure layer.
/// </summary>
public interface IAuditService
{
    Task LogAsync(
        string entityName,
        string entityId,
        string action,
        object? oldValues = null,
        object? newValues = null,
        Guid? correlationId = null,
        CancellationToken ct = default);
}
