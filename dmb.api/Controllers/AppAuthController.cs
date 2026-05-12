using Dmb.Model.Dtos;
using Dmb.Model.Enums;
using Dmb.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dmb.api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppAuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AppAuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto model, CancellationToken cancellationToken)
    {
        var result = await _authService.LoginForAppAsync(model, cancellationToken);
        return result.Status switch
        {
            AuthTokenLoginStatus.AccountBlocked =>
                StatusCode(StatusCodes.Status403Forbidden, new { message = result.BlockReason }),
            AuthTokenLoginStatus.Success =>
                Ok(new
                {
                    accessToken = result.AccessToken,
                    refreshToken = result.RefreshToken,
                    isPinSet = result.IsPinSet
                }),
            _ => Unauthorized()
        };
    }

    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] AppRefreshTokenRequestDto request, CancellationToken cancellationToken)
    {
        var result = await _authService.RefreshAppTokenAsync(request, cancellationToken);
        return result.Status switch
        {
            AuthTokenLoginStatus.Success => Ok(new
            {
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                isPinSet = result.IsPinSet
            }),
            AuthTokenLoginStatus.ExpiredRefreshToken => Unauthorized(new { message = "Refresh token is expired." }),
            _ => Unauthorized(new { message = "Invalid token pair." })
        };
    }

    [Authorize]
    [HttpPost("setpin")]
    public async Task<IActionResult> SetUserPin([FromBody] SetUserPinRequestDto request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid user context." });
        }

        if (string.IsNullOrWhiteSpace(request.Pin) || request.Pin.Length < 4)
        {
            return BadRequest(new { message = "PIN must be at least 4 digits." });
        }

        var ok = await _authService.SetUserPinAsync(userId, request.Pin, cancellationToken);
        return ok ? Ok(new { message = "PIN set successfully." }) : NotFound(new { message = "User not found." });
    }

    [Authorize]
    [HttpPost("verify")]
    public async Task<IActionResult> VerifyUserPin([FromBody] VerifyUserPinRequestDto request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid user context." });
        }

        var isValid = await _authService.VerifyUserPinAsync(userId, request.Pin, cancellationToken);
        return isValid ? Ok(new { message = "PIN verified." }) : Unauthorized(new { message = "Invalid PIN." });
    }
}
