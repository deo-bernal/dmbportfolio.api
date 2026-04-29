using Dmb.Model.Dtos;
using Dmb.Service.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IAuthService _authService;
    private readonly IMemoryCache _cache;

    public AuthController(IConfiguration configuration, IAuthService authService, IMemoryCache cache)
    {
        _configuration = configuration;
        _authService = authService;
        _cache = cache;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginDto model, CancellationToken cancellationToken)
    {
        var loggedInUser = await _authService.AuthenticateAsync(model.Username, model.Password, cancellationToken);
        if (loggedInUser is not null)
        {
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Name, loggedInUser.Username),
                new Claim(ClaimTypes.NameIdentifier, loggedInUser.UserId.ToString()),
                new Claim(ClaimTypes.Email, loggedInUser.Email)
            };

            var secret = _configuration["Jwt:Secret"]
                ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
            var issuer = _configuration["Jwt:Issuer"] ?? "dmbapp";
            var audience = _configuration["Jwt:Audience"] ?? "dmbapp";

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: DateTime.Now.AddHours(1),
                signingCredentials: creds
            );

            return Ok(new { token = new JwtSecurityTokenHandler().WriteToken(token) });
        }

        return Unauthorized();
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterDto request, CancellationToken cancellationToken)
    {
        if (await _authService.UsernameExistsAsync(request.Username, cancellationToken))
        {
            return BadRequest("Username already exists.");
        }

        await _authService.RegisterAsync(request, cancellationToken);

        return Ok("User registered successfully.");
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        var username = User.Identity?.Name;
        var jti = User.FindFirstValue(JwtRegisteredClaimNames.Jti);
        var expiresAt = User.FindFirstValue(JwtRegisteredClaimNames.Exp);

        if (!string.IsNullOrWhiteSpace(jti))
        {
            var jwtExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
            if (long.TryParse(expiresAt, out var expSeconds))
            {
                jwtExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
            }

            var ttl = jwtExpiresAt - DateTimeOffset.UtcNow;
            if (ttl < TimeSpan.Zero)
            {
                ttl = TimeSpan.FromMinutes(5);
            }

            _cache.Set($"revoked_jti:{jti}", true, ttl);
            await _authService.RevokeJtiAsync(jti, jwtExpiresAt, cancellationToken);
        }

        Response.Headers.CacheControl = "no-store, no-cache, must-revalidate";
        Response.Headers.Pragma = "no-cache";

        return Ok(new
        {
            message = "Logged out successfully.",
            username
        });
    }
}