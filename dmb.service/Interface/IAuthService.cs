using Dmb.Model.Dtos;
using Dmb.Model.Enums;

namespace Dmb.Service.Interface;

public interface IAuthService
{
    Task<AuthTokenLoginResult> LoginWithJwtAsync(LoginDto model, CancellationToken cancellationToken = default);
    Task<AuthTokenLoginResult> LoginForAppAsync(LoginDto model, CancellationToken cancellationToken = default);
    Task<AuthTokenLoginResult> RefreshAppTokenAsync(AppRefreshTokenRequestDto request, CancellationToken cancellationToken = default);
    Task<bool> SetUserPinAsync(int userId, string pin, CancellationToken cancellationToken = default);
    Task<bool> VerifyUserPinAsync(int userId, string pin, CancellationToken cancellationToken = default);
    Task<LogoutWorkflowResult> LogoutAsync(string? username, string? jti, string? expClaim, int? userId = null, CancellationToken cancellationToken = default);

    Task<ForgotPasswordRequestStatus> RequestPasswordResetAsync(ForgotPasswordDto request, CancellationToken cancellationToken = default);
    Task<PasswordResetCompletionStatus> CompletePasswordResetAsync(ResetPasswordDto request, CancellationToken cancellationToken = default);

    Task<LoginResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> EmailAlreadyRegisteredAsync(string email, CancellationToken cancellationToken = default);
    Task<bool> UsernameFirstLastExistsAsync(string username, string firstName, string lastName, CancellationToken cancellationToken = default);
    Task<LoggedInUserDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default);
    Task<bool> IsJtiRevokedAsync(string jti, CancellationToken cancellationToken = default);
    Task RevokeJtiAsync(string jti, DateTimeOffset expiresAt, int? userId = null, CancellationToken cancellationToken = default);
    string HashPassword(string password, string passwordSalt);
    bool VerifyPassword(string password, string passwordSalt, string passwordHash);
    (string PasswordHash, string PasswordSalt) CreatePasswordHash(string password);
}
