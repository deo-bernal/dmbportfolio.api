using Dmb.Model.Dtos;
using Dmb.Service.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/auth/external")]
public class ExternalAuthController : ControllerBase
{
    private readonly IExternalAuthService _externalAuthService;
    private readonly IConfiguration _configuration;

    public ExternalAuthController(IExternalAuthService externalAuthService, IConfiguration configuration)
    {
        _externalAuthService = externalAuthService;
        _configuration = configuration;
    }

    [HttpGet("{provider}/start")]
    [AllowAnonymous]
    public IActionResult Start(string provider, [FromQuery] string? client, [FromQuery] string? redirect)
    {
        var result = _externalAuthService.Start(provider, client, redirect, BuildCallbackUrl(provider));
        if (!string.IsNullOrWhiteSpace(result.RedirectUrl))
        {
            return Redirect(result.RedirectUrl);
        }

        return StatusCode(StatusCodes.Status503ServiceUnavailable, new
        {
            message = result.ErrorMessage ?? "Social sign-in is not available."
        });
    }

    [HttpGet("{provider}/callback")]
    [AllowAnonymous]
    public async Task<IActionResult> Callback(
        string provider,
        [FromQuery] string? code,
        [FromQuery] string? state,
        [FromQuery] string? error,
        [FromQuery(Name = "error_description")] string? errorDescription,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(state) &&
            (state.StartsWith("crm.", StringComparison.Ordinal) ||
             state.StartsWith("lms.", StringComparison.Ordinal) ||
             state.StartsWith("commerce.", StringComparison.Ordinal) ||
             state.StartsWith("agent.", StringComparison.Ordinal)))
        {
            var providerKey = provider.Trim().ToLowerInvariant();
            var query = HttpContext.Request.QueryString.HasValue
                ? HttpContext.Request.QueryString.Value
                : "";
            var workspace = state.StartsWith("agent.", StringComparison.Ordinal)
                ? "agent"
                : state.StartsWith("commerce.", StringComparison.Ordinal)
                ? "commerce"
                : state.StartsWith("lms.", StringComparison.Ordinal) ? "lms" : "crm";
            // Stay on the public site. A hop to *.onrender.com is intercepted as "Dangerous site".
            return Redirect($"https://www.dmbwebsolutions.com/{workspace}/api/auth/external/{providerKey}/callback{query}");
        }

        var redirectUrl = await _externalAuthService.HandleCallbackAsync(
            provider,
            code,
            state,
            error,
            errorDescription,
            BuildCallbackUrl(provider),
            cancellationToken);
        return Redirect(redirectUrl);
    }

    [HttpPost("complete")]
    [AllowAnonymous]
    public async Task<IActionResult> Complete(
        [FromBody] ExternalAuthCompleteRequest request,
        CancellationToken cancellationToken)
    {
        var (ok, message, statusCode) = await _externalAuthService.CompleteAsync(request, cancellationToken);
        return StatusCode(statusCode, new { message });
    }

    [HttpPost("verify")]
    [AllowAnonymous]
    public async Task<IActionResult> Verify(
        [FromBody] ExternalAuthVerifyRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _externalAuthService.VerifyAsync(request, cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
        }

        if (string.Equals(result.Client, "app", StringComparison.OrdinalIgnoreCase))
        {
            return Ok(new
            {
                accessToken = result.AccessToken,
                refreshToken = result.RefreshToken,
                isPinSet = result.IsPinSet
            });
        }

        return Ok(new { token = result.AccessToken });
    }

    private string BuildCallbackUrl(string provider)
    {
        var providerKey = provider.Trim().ToLowerInvariant();
        var frontend = (_configuration["App:FrontendUrl"] ?? "").Trim().TrimEnd('/');
        var isLocal = string.IsNullOrWhiteSpace(frontend)
            || frontend.Contains("localhost", StringComparison.OrdinalIgnoreCase);

        if (!isLocal)
        {
            if (frontend.Contains("onrender.com", StringComparison.OrdinalIgnoreCase))
            {
                frontend = "https://www.dmbwebsolutions.com";
            }

            // Facebook rejects *.onrender.com. LinkedIn's return to Render is blocked by Chrome.
            if (providerKey is "facebook" or "linkedin")
            {
                return $"{frontend}/api/auth/external/{providerKey}/callback";
            }

            var publicApi = (_configuration["App:PublicApiUrl"] ?? "").Trim().TrimEnd('/');
            if (!string.IsNullOrWhiteSpace(publicApi))
            {
                return $"{publicApi}/auth/external/{providerKey}/callback";
            }
        }

        var pathBase = HttpContext.Request.PathBase.HasValue ? HttpContext.Request.PathBase.Value : "";
        return $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{pathBase}/api/auth/external/{providerKey}/callback";
    }
}
