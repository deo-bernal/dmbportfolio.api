using Dmb.Data.Context;
using Dmb.Model.Dtos;
using Dmb.Service.Interface;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IAuthService _authService;
    private readonly IMemoryCache _cache;
    private readonly DmbDbContext _context;
    private readonly IEmailService _emailService;

    public AuthController(
        IConfiguration configuration,
        IAuthService authService,
        IMemoryCache cache,
        DmbDbContext context,
        IEmailService emailService)
    {
        _configuration = configuration;
        _authService = authService;
        _cache = cache;
        _context = context;
        _emailService = emailService;
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

    [HttpPost("forgot-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordDto request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim();
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null)
        {
            return Ok(new { message = "If that email exists, a reset link has been sent." });
        }

        var existingTokens = await _context.PasswordResetTokens
            .Where(t => t.UserId == user.UserId)
            .ToListAsync(cancellationToken);
        if (existingTokens.Count > 0)
        {
            _context.PasswordResetTokens.RemoveRange(existingTokens);
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var resetToken = new Dmb.Data.Entities.PasswordResetToken
        {
            UserId = user.UserId,
            Token = token,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            IsUsed = false
        };

        _context.PasswordResetTokens.Add(resetToken);
        await _context.SaveChangesAsync(cancellationToken);

        var frontendUrl = (_configuration["App:FrontendUrl"] ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            throw new InvalidOperationException("App:FrontendUrl is not configured.");
        }

        var resetLink = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";

        try
        {
            await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink, cancellationToken);
        }
        catch
        {
            _context.PasswordResetTokens.Remove(resetToken);
            await _context.SaveChangesAsync(cancellationToken);
            return StatusCode(StatusCodes.Status503ServiceUnavailable,
                new { message = "Unable to send the reset email. Please try again later." });
        }

        return Ok(new { message = "If that email exists, a reset link has been sent." });
    }

    [HttpPost("reset-password")]
    [AllowAnonymous]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordDto request, CancellationToken cancellationToken)
    {
        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return BadRequest(new { message = "Password and confirmation do not match." });
        }

        var resetToken = await _context.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

        if (resetToken is null || resetToken.IsUsed || resetToken.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return BadRequest(new { message = "Reset link is invalid or has expired." });
        }

        var (passwordHash, passwordSalt) = _authService.CreatePasswordHash(request.NewPassword);
        resetToken.User.PasswordHash = passwordHash;
        resetToken.User.PasswordSalt = passwordSalt;

        _context.PasswordResetTokens.Remove(resetToken);
        await _context.SaveChangesAsync(cancellationToken);

        return Ok(new { message = "Password reset successfully." });
    }
}