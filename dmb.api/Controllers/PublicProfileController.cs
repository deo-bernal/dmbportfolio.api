using Dmb.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace dmb.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublicProfileController : ControllerBase
    {
        private readonly IDmbReadService _dmbReadService;

        public PublicProfileController(IDmbReadService dmbReadService)
        {
            _dmbReadService = dmbReadService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? username, CancellationToken cancellationToken)
        {
            var profile = await _dmbReadService.GetPublicProfileAsync(username, cancellationToken);
            if (profile is null)
            {
                return NotFound(new { message = "No publicly viewable profile found." });
            }

            return Ok(profile);
        }
    }
}
