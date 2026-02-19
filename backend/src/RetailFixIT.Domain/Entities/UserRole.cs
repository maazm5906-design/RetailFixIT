using RetailFixIT.Domain.Common;
using RetailFixIT.Domain.Enums;

namespace RetailFixIT.Domain.Entities;

public class UserRole : BaseEntity
{
    public string UserId { get; set; } = string.Empty; // Azure AD OID or local user ID
    public string Email { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public UserRoleType Role { get; set; }
    public bool IsActive { get; set; } = true;

    public Tenant Tenant { get; set; } = null!;
}
