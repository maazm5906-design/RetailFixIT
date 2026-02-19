using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailFixIT.Application.Common.Interfaces;
using RetailFixIT.Domain.Enums;
using RetailFixIT.Infrastructure.Auth;

namespace RetailFixIT.API.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController : ControllerBase
{
    private readonly JwtTokenService _jwt;
    private readonly ICurrentUserService _user;

    public AuthController(JwtTokenService jwt, ICurrentUserService user)
    {
        _jwt = jwt;
        _user = user;
    }

    /// <summary>
    /// Dev-only: Login with pre-seeded test accounts
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    public IActionResult Login([FromBody] LoginRequest request)
    {
        // Hardcoded test users for development
        var testUsers = new[]
        {
            new { Email = "dispatcher@acme.com", Password = "Test1234!", Name = "Alice Dispatcher", Role = UserRoleType.Dispatcher, TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111") },
            new { Email = "vendormgr@acme.com", Password = "Test1234!", Name = "Bob VendorMgr", Role = UserRoleType.VendorManager, TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111") },
            new { Email = "admin@acme.com", Password = "Test1234!", Name = "Carol Admin", Role = UserRoleType.Admin, TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111") },
            new { Email = "support@acme.com", Password = "Test1234!", Name = "Dave Support", Role = UserRoleType.SupportAgent, TenantId = Guid.Parse("11111111-1111-1111-1111-111111111111") },
        };

        var testUser = testUsers.FirstOrDefault(u =>
            u.Email.Equals(request.Email, StringComparison.OrdinalIgnoreCase) && u.Password == request.Password);

        if (testUser == null)
            return Unauthorized(new { error = "Invalid credentials" });

        var token = _jwt.GenerateToken(testUser.Email, testUser.Email, testUser.Name, testUser.Role, testUser.TenantId);
        return Ok(new
        {
            accessToken = token,
            user = new
            {
                email = testUser.Email,
                name = testUser.Name,
                role = testUser.Role.ToString(),
                tenantId = testUser.TenantId
            }
        });
    }

    [HttpGet("me")]
    [Authorize]
    public IActionResult Me()
    {
        return Ok(new
        {
            userId = _user.UserId,
            email = _user.Email,
            displayName = _user.DisplayName,
            role = _user.Role.ToString(),
            tenantId = _user.TenantId
        });
    }
}

public record LoginRequest(string Email, string Password);
