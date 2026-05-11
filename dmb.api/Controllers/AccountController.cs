using Dmb.Service.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dmb.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class AccountController : ControllerBase
{
    private readonly IDmbReadService _dmbReadService;

    public AccountController(IDmbReadService dmbReadService)
    {
        _dmbReadService = dmbReadService;
    }

    [HttpDelete]
    public async Task<IActionResult> DeleteAccount(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid user context." });
        }

        var deleted = await _dmbReadService.DeleteAccountAsync(userId, cancellationToken);
        if (!deleted)
        {
            return NotFound(new { message = "User account not found." });
        }

        return Ok(new { message = "Account deleted successfully." });
    }
}

