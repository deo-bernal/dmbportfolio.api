using System.Security.Cryptography;
using System.Text;
using Dmb.Data.Repository.Interface;
using Dmb.Model.Dtos;
using Dmb.Service.Interface;

namespace Dmb.Service.Implementation;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;

    public AuthService(IAuthRepository authRepository)
    {
        _authRepository = authRepository;
    }

    public async Task<LoggedInUserDto?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        var user = await _authRepository.GetUserRecordAsync(username, cancellationToken);
        if (user is null)
        {
            return null;
        }

        var isPasswordValid = VerifyPassword(password, user.PasswordSalt, user.PasswordHash);
        if (!isPasswordValid)
        {
            return null;
        }

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

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        return _authRepository.UsernameExistsAsync(username, cancellationToken);
    }

    public Task<bool> IsJtiRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        return _authRepository.IsJtiRevokedAsync(jti, cancellationToken);
    }

    public Task RevokeJtiAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        return _authRepository.RevokeJtiAsync(jti, expiresAt, cancellationToken);
    }

    public async Task<LoggedInUserDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default)
    {
        var (passwordHash, passwordSalt) = CreatePasswordHash(request.Password);

        var user = await _authRepository.CreateUserAsync(new UserDto
        {
            Username = request.Username,
            PasswordHash = passwordHash,
            PasswordSalt = passwordSalt,
            FirstName = request.FirstName,
            LastName = request.LastName,
            Email = request.Email,
            ContactNo = request.ContactNo,
            Activated = true,
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
}
