using Dmb.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace dmb.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ProfileDetailsController : ControllerBase
{
    private readonly IDmbReadService _dmbReadService;

    public ProfileDetailsController(IDmbReadService dmbReadService)
    {
        _dmbReadService = dmbReadService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyProfile(CancellationToken cancellationToken)
    {
        try
        {
            var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdValue, out var userId))
            {
                return Unauthorized(new { message = "Invalid user context." });
            }

            var userCompleteDetails = await _dmbReadService.GetUserCompleteDetailsAsync(userId, cancellationToken);
            if (userCompleteDetails is null)
            {
                return NotFound(new { message = "User profile not found." });
            }

            return Ok(userCompleteDetails);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Request was canceled by the caller/client. Return a non-error response.
            return new EmptyResult();
        }
    }
}
