using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using RetailFixIT.Application.Common.Interfaces;
using RetailFixIT.Domain.Enums;

namespace RetailFixIT.Infrastructure.Services;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    private ClaimsPrincipal? User => _httpContextAccessor.HttpContext?.User;

    // Azure AD: oid claim. Dev JWT: NameIdentifier.
    public string UserId =>
        User?.FindFirstValue("http://schemas.microsoft.com/identity/claims/objectidentifier")
        ?? User?.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? string.Empty;

    // Azure AD: preferred_username. Dev JWT: email claim.
    public string Email =>
        User?.FindFirstValue("preferred_username")
        ?? User?.FindFirstValue(ClaimTypes.Email)
        ?? string.Empty;

    // Both paths map 'name' to ClaimTypes.Name.
    public string DisplayName => User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated ?? false;

    // Azure AD: Microsoft.Identity.Web maps roles[] entries to ClaimTypes.Role.
    // Dev JWT: role written directly to ClaimTypes.Role.
    public UserRoleType Role
    {
        get
        {
            var roleStr = User?.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
            return Enum.TryParse<UserRoleType>(roleStr, out var role) ? role : UserRoleType.SupportAgent;
        }
    }

    // Dev JWT: custom tenant_id claim. Azure AD: tid mapped by Microsoft.Identity.Web.
    public Guid TenantId
    {
        get
        {
            var claim = User?.FindFirstValue("tenant_id")
                ?? User?.FindFirstValue("http://schemas.microsoft.com/identity/claims/tenantid");
            return Guid.TryParse(claim, out var tid) ? tid : Guid.Empty;
        }
    }
}
