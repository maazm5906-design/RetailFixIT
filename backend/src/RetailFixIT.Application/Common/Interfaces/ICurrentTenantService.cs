namespace RetailFixIT.Application.Common.Interfaces;

/// <summary>
/// Provides the current tenant context resolved from the JWT claim.
/// Implemented in Infrastructure layer as scoped per request.
/// </summary>
public interface ICurrentTenantService
{
    Guid TenantId { get; }
    void SetTenantId(Guid tenantId);
}
