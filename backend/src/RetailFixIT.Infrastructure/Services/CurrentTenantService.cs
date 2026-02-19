using RetailFixIT.Application.Common.Interfaces;

namespace RetailFixIT.Infrastructure.Services;

/// <summary>
/// Implements ICurrentTenantService from Application layer.
/// Registered as scoped — set once per HTTP request by TenantResolutionMiddleware.
/// </summary>
public class CurrentTenantService : ICurrentTenantService
{
    private Guid _tenantId = Guid.Empty;

    public Guid TenantId => _tenantId;

    public void SetTenantId(Guid tenantId) => _tenantId = tenantId;
}
