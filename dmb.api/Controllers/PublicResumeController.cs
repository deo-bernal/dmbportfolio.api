using Dmb.Service.Interface;
using Microsoft.AspNetCore.Mvc;

namespace dmb.api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PublicResumeController : ControllerBase
    {
        private readonly IDmbReadService _dmbReadService;

        public PublicResumeController(IDmbReadService dmbReadService)
        {
            _dmbReadService = dmbReadService;
        }

        [HttpGet]
        public async Task<IActionResult> Get([FromQuery] string? username, CancellationToken cancellationToken)
        {
            var resume = await _dmbReadService.GetPublicResumeAsync(username, cancellationToken);
            if (resume is null)
            {
                return NotFound(new { message = "No publicly viewable resume found." });
            }

            return Ok(resume);
        }
    }
}

