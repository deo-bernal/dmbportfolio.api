using Dmb.Model.Enums;
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
        var outcome = await _dmbReadService.GetMyProfileAsync(
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
            cancellationToken);

        return outcome.Status switch
        {
            MyProfileWorkflowStatus.InvalidUserContext => Unauthorized(new { message = "Invalid user context." }),
            MyProfileWorkflowStatus.NotFound => NotFound(new { message = "User profile not found." }),
            MyProfileWorkflowStatus.Canceled => new EmptyResult(),
            _ => Ok(outcome.Details)
        };
    }

    [HttpPut]
    public async Task<IActionResult> UpdateMyProfile([FromBody] UpdateMyProfileDto request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid user context." });
        }

        var updated = await _dmbReadService.UpdateMyProfileAsync(userId, request, cancellationToken);
        if (!updated)
        {
            return NotFound(new { message = "User profile not found." });
        }

        return Ok(new { message = "Profile updated successfully." });
    }

    [HttpPost]
    public async Task<IActionResult> CreateMyProfile([FromBody] UpdateMyProfileDto request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid user context." });
        }

        var outcome = await _dmbReadService.CreateMyProfileAsync(userId, request, cancellationToken);
        return outcome switch
        {
            CreateMyProfileStatus.NotFound => NotFound(new { message = "User profile not found." }),
            CreateMyProfileStatus.AlreadyExists => Conflict(new { message = "User profile already exists." }),
            _ => Ok(new { message = "Profile created successfully." })
        };
    }
}
