using Dmb.Model.Dtos;
using Dmb.Model.Enums;
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

    [HttpPut("users/{userId:int}")]
    public async Task<IActionResult> UpdateUser(
        int userId,
        [FromBody] UpdateAdminUserRequestDto request,
        CancellationToken cancellationToken)
    {
        if (!await IsSuperAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized(new { message = "Invalid user context." });
        }

        var status = await _dmbReadService.TryUpdateAdminUserAsync(actorUserId, userId, request, cancellationToken);
        return status switch
        {
            AdminUserMutationStatus.Ok => Ok(new { message = "User updated.", userId }),
            AdminUserMutationStatus.NotFound => NotFound(new { message = "User not found." }),
            AdminUserMutationStatus.Forbidden => StatusCode(StatusCodes.Status403Forbidden, new { message = "That user cannot be updated." }),
            _ => Conflict(new { message = "That email is already in use, or the name and email are required." }),
        };
    }

    [HttpDelete("users/{userId:int}")]
    public async Task<IActionResult> DeleteUser(int userId, CancellationToken cancellationToken)
    {
        if (!await IsSuperAdminAsync(cancellationToken))
        {
            return Forbid();
        }

        if (!TryGetActorUserId(out var actorUserId))
        {
            return Unauthorized(new { message = "Invalid user context." });
        }

        var status = await _dmbReadService.TryDeleteAdminUserAsync(actorUserId, userId, cancellationToken);
        return status switch
        {
            AdminUserMutationStatus.Ok => Ok(new { message = "User and all related records were deleted.", userId }),
            AdminUserMutationStatus.NotFound => NotFound(new { message = "User not found." }),
            _ => StatusCode(StatusCodes.Status403Forbidden, new { message = "You cannot delete your own account or a super admin from here." }),
        };
    }

    private bool TryGetActorUserId(out int userId)
    {
        return int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out userId);
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
