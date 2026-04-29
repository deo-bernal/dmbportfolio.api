using Dmb.Model.Dtos;

namespace Dmb.Service.Interface;

public interface IAuthService
{
    Task<LoggedInUserDto?> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
    Task<LoggedInUserDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default);
    Task<bool> IsJtiRevokedAsync(string jti, CancellationToken cancellationToken = default);
    Task RevokeJtiAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
    string HashPassword(string password, string passwordSalt);
    bool VerifyPassword(string password, string passwordSalt, string passwordHash);
    (string PasswordHash, string PasswordSalt) CreatePasswordHash(string password);
}
