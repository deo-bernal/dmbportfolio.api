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
}
