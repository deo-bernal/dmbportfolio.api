using System.Security.Claims;
using Dmb.Model.Dtos;
using Dmb.Service.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace dmb.api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
public class ResumeController : ControllerBase
{
    private readonly IDmbReadService _dmbReadService;

    public ResumeController(IDmbReadService dmbReadService)
    {
        _dmbReadService = dmbReadService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyResume(CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid user context." });
        }

        var resume = await _dmbReadService.GetMyResumeAsync(userId, cancellationToken);
        if (resume is null)
        {
            return NotFound(new { message = "Resume profile not found." });
        }

        return Ok(resume);
    }

    [HttpPut]
    public async Task<IActionResult> UpsertMyResume([FromBody] UpdateResumeDto request, CancellationToken cancellationToken)
    {
        if (!int.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId))
        {
            return Unauthorized(new { message = "Invalid user context." });
        }

        var updated = await _dmbReadService.UpsertMyResumeAsync(userId, request, cancellationToken);
        if (!updated)
        {
            return NotFound(new { message = "Resume profile not found." });
        }

        return Ok(new { message = "Resume updated successfully." });
    }
}
