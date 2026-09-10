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
        CancellationToken cancellationToken)
    {
        var redirectUrl = await _externalAuthService.HandleCallbackAsync(
            provider,
            code,
            state,
            error,
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
        var publicApi = (_configuration["App:PublicApiUrl"] ?? "").Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(publicApi))
        {
            return $"{publicApi}/auth/external/{provider.Trim().ToLowerInvariant()}/callback";
        }

        var pathBase = HttpContext.Request.PathBase.HasValue ? HttpContext.Request.PathBase.Value : "";
        return $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}{pathBase}/api/auth/external/{provider.Trim().ToLowerInvariant()}/callback";
    }
}
