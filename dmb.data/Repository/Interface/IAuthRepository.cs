using Dmb.Model.Dtos;

namespace Dmb.Data.Repository.Interface;

public interface IAuthRepository
{
    Task<AuthTokenLoginResult> LoginAndIssueJwtAsync(LoginDto model, CancellationToken cancellationToken = default);
    Task<LogoutWorkflowResult> LogoutAsync(string? username, string? jti, string? expClaim, CancellationToken cancellationToken = default);

    Task<UserDto?> GetUserRecordAsync(string username, CancellationToken cancellationToken = default);
    Task<LoginResult> LoginAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> EmailAlreadyRegisteredAsync(string email, CancellationToken cancellationToken = default);
    Task<UserDto> CreateUserAsync(UserDto user, CancellationToken cancellationToken = default);
    Task<LoggedInUserDto> CreateRegisteredUserAsync(RegisterDto request, CancellationToken cancellationToken = default);
    Task<bool> IsJtiRevokedAsync(string jti, CancellationToken cancellationToken = default);
    Task RevokeJtiAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
    (string PasswordHash, string PasswordSalt) CreatePasswordHash(string password);
    string HashPassword(string password, string passwordSalt);
    bool VerifyPassword(string password, string passwordSalt, string passwordHash);
}
