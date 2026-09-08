using Dmb.Model.Dtos;
using Dmb.Service.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dmb.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AdminController : ControllerBase
{
    private readonly IDmbReadService _dmbReadService;

    public AdminController(IDmbReadService dmbReadService)
    {
        _dmbReadService = dmbReadService;
    }

    [HttpGet("users")]
    public async Task<IActionResult> ListUsers(CancellationToken cancellationToken)
    {
        if (!await IsSuperAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var users = await _dmbReadService.ListAdminUsersAsync(cancellationToken);
        return Ok(users);
    }

    [HttpPatch("users/{userId:int}")]
    public async Task<IActionResult> SetUserAdmin(
        int userId,
        [FromBody] SetUserAdminRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!await IsSuperAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        var updated = await _dmbReadService.TrySetUserIsAdminAsync(userId, request.IsAdmin, cancellationToken);
        if (!updated)
        {
            return NotFound(new { message = "User not found." });
        }

        return Ok(new { message = "Admin access updated.", userId, isAdmin = request.IsAdmin });
    }

    private async Task<bool> IsSuperAdminAsync(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return false;
        }

        return await _dmbReadService.UserIsSuperAdminAsync(userId, cancellationToken);
    }
}
