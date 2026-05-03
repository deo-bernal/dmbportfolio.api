using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using AutoMapper;
using Dmb.Data.Context;
using Dmb.Data.Entities;
using Dmb.Data.Repository.Interface;
using Dmb.Model.Dtos;
using Dmb.Model.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace Dmb.Data.Repository.Implementation;

public class AuthRepository : IAuthRepository
{
    private readonly DmbDbContext _dbContext;
    private readonly IMapper _mapper;
    private readonly IConfiguration _configuration;
    private readonly IMemoryCache _cache;

    public AuthRepository(
        DmbDbContext dbContext,
        IMapper mapper,
        IConfiguration configuration,
        IMemoryCache cache)
    {
        _dbContext = dbContext;
        _mapper = mapper;
        _configuration = configuration;
        _cache = cache;
    }

    public async Task<AuthTokenLoginResult> LoginAndIssueJwtAsync(LoginDto model, CancellationToken cancellationToken = default)
    {
        var login = await LoginAsync(model.Username, model.Password, cancellationToken);
        if (!string.IsNullOrEmpty(login.BlockReason))
        {
            return new AuthTokenLoginResult
            {
                Status = AuthTokenLoginStatus.AccountBlocked,
                BlockReason = login.BlockReason
            };
        }

        if (login.User is null)
        {
            return new AuthTokenLoginResult { Status = AuthTokenLoginStatus.InvalidCredentials };
        }

        var accessToken = CreateAccessToken(login.User);
        return new AuthTokenLoginResult
        {
            Status = AuthTokenLoginStatus.Success,
            AccessToken = accessToken
        };
    }

    public async Task<LogoutWorkflowResult> LogoutAsync(
        string? username,
        string? jti,
        string? expClaim,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(jti))
        {
            var jwtExpiresAt = DateTimeOffset.UtcNow.AddHours(1);
            if (long.TryParse(expClaim, out var expSeconds))
            {
                jwtExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expSeconds);
            }

            var ttl = jwtExpiresAt - DateTimeOffset.UtcNow;
            if (ttl < TimeSpan.Zero)
            {
                ttl = TimeSpan.FromMinutes(5);
            }

            _cache.Set($"revoked_jti:{jti}", true, ttl);
            await RevokeJtiAsync(jti, jwtExpiresAt, cancellationToken);
        }

        return new LogoutWorkflowResult { Username = username };
    }

    private string CreateAccessToken(LoggedInUserDto user)
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        var issuer = _configuration["Jwt:Issuer"] ?? "dmbapp";
        var audience = _configuration["Jwt:Audience"] ?? "dmbapp";

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
            new Claim(ClaimTypes.Email, user.Email)
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddHours(1),
            signingCredentials: creds);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<LoginResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await GetUserRecordAsync(username, cancellationToken);
        if (user is null)
        {
            return new LoginResult();
        }

        if (!VerifyPassword(password, user.PasswordSalt, user.PasswordHash))
        {
            return new LoginResult();
        }

        if (!user.Activated)
        {
            return new LoginResult
            {
                BlockReason = "Your account is not activated yet. Use the activation link we emailed you."
            };
        }

        return new LoginResult
        {
            User = new LoggedInUserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Activated = user.Activated,
                CreatedAt = user.CreatedAt
            }
        };
    }

    public async Task<LoggedInUserDto> CreateRegisteredUserAsync(RegisterDto request, CancellationToken cancellationToken = default)
    {
        var (passwordHash, passwordSalt) = CreatePasswordHash(request.Password);
        var email = request.Email.Trim().ToLowerInvariant();

        var user = await CreateUserAsync(new UserDto
        {
            Username = email,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = email,
            ContactNo = request.ContactNumber.Trim(),
            Activated = false,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);

        return new LoggedInUserDto
        {
            UserId = user.UserId,
            Username = user.Username,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            Activated = user.Activated,
            CreatedAt = user.CreatedAt
        };
    }

    public (string PasswordHash, string PasswordSalt) CreatePasswordHash(string password)
    {
        using var hmac = new HMACSHA512();
        var passwordSalt = Convert.ToBase64String(hmac.Key);
        var passwordHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(password)));
        return (passwordHash, passwordSalt);
    }

    public string HashPassword(string password, string passwordSalt)
    {
        var saltBytes = Convert.FromBase64String(passwordSalt);
        using var hmac = new HMACSHA512(saltBytes);
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string passwordSalt, string passwordHash)
    {
        var computedHash = HashPassword(password, passwordSalt);
        return CryptographicOperations.FixedTimeEquals(
            Convert.FromBase64String(computedHash),
            Convert.FromBase64String(passwordHash));
    }

    public async Task<UserDto?> GetUserRecordAsync(string username, CancellationToken cancellationToken = default)
    {
        var normalized = username.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                u => u.Username.ToLower() == normalized || u.Email.ToLower() == normalized,
                cancellationToken);

        return user is null ? null : _mapper.Map<UserDto>(user);
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        var u = username.Trim().ToLowerInvariant();
        return _dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Username.ToLower() == u, cancellationToken);
    }

    public Task<bool> EmailAlreadyRegisteredAsync(string email, CancellationToken cancellationToken = default)
    {
        var e = email.Trim().ToLowerInvariant();
        return _dbContext.Users
            .AsNoTracking()
            .AnyAsync(x => x.Email.ToLower() == e || x.Username.ToLower() == e, cancellationToken);
    }

    public async Task<UserDto> CreateUserAsync(UserDto user, CancellationToken cancellationToken = default)
    {
        var entity = new User
        {
            Username = user.Username,
            PasswordHash = user.PasswordHash,
            PasswordSalt = user.PasswordSalt,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            ContactNo = user.ContactNo,
            Activated = user.Activated,
            CreatedAt = user.CreatedAt
        };

        _dbContext.Users.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(entity);
    }

    public Task<bool> IsJtiRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        return _dbContext.RevokedTokens
            .AsNoTracking()
            .AnyAsync(revokedToken => revokedToken.Jti == jti, cancellationToken);
    }

    public async Task RevokeJtiAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var alreadyRevoked = await IsJtiRevokedAsync(jti, cancellationToken);
        if (alreadyRevoked)
        {
            return;
        }

        _dbContext.RevokedTokens.Add(new RevokedToken
        {
            Jti = jti,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
