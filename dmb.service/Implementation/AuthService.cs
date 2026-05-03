using Dmb.Data.Repository.Interface;
using Dmb.Model.Dtos;
using Dmb.Model.Enums;
using Dmb.Service.Interface;

namespace Dmb.Service.Implementation;

public class AuthService : IAuthService
{
    private readonly IAuthRepository _authRepository;
    private readonly IPasswordResetRepository _passwordResetRepository;

    public AuthService(IAuthRepository authRepository, IPasswordResetRepository passwordResetRepository)
    {
        _authRepository = authRepository;
        _passwordResetRepository = passwordResetRepository;
    }

    public Task<AuthTokenLoginResult> LoginWithJwtAsync(LoginDto model, CancellationToken cancellationToken = default)
    {
        return _authRepository.LoginAndIssueJwtAsync(model, cancellationToken);
    }

    public Task<LogoutWorkflowResult> LogoutAsync(
        string? username,
        string? jti,
        string? expClaim,
        CancellationToken cancellationToken = default)
    {
        return _authRepository.LogoutAsync(username, jti, expClaim, cancellationToken);
    }

    public Task<ForgotPasswordRequestStatus> RequestPasswordResetAsync(
        ForgotPasswordDto request,
        CancellationToken cancellationToken = default)
    {
        return _passwordResetRepository.RequestPasswordResetAsync(request, cancellationToken);
    }

    public Task<PasswordResetCompletionStatus> CompletePasswordResetAsync(
        ResetPasswordDto request,
        CancellationToken cancellationToken = default)
    {
        return _passwordResetRepository.CompletePasswordResetAsync(request, cancellationToken);
    }

    public Task<LoginResult> AuthenticateAsync(string username, string password, CancellationToken cancellationToken = default)
    {
        return _authRepository.LoginAsync(username, password, cancellationToken);
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        return _authRepository.UsernameExistsAsync(username, cancellationToken);
    }

    public Task<bool> EmailAlreadyRegisteredAsync(string email, CancellationToken cancellationToken = default)
    {
        return _authRepository.EmailAlreadyRegisteredAsync(email, cancellationToken);
    }

    public Task<bool> IsJtiRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        return _authRepository.IsJtiRevokedAsync(jti, cancellationToken);
    }

    public Task RevokeJtiAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        return _authRepository.RevokeJtiAsync(jti, expiresAt, cancellationToken);
    }

    public Task<LoggedInUserDto> RegisterAsync(RegisterDto request, CancellationToken cancellationToken = default)
    {
        return _authRepository.CreateRegisteredUserAsync(request, cancellationToken);
    }

    public (string PasswordHash, string PasswordSalt) CreatePasswordHash(string password)
    {
        return _authRepository.CreatePasswordHash(password);
    }

    public string HashPassword(string password, string passwordSalt)
    {
        return _authRepository.HashPassword(password, passwordSalt);
    }

    public bool VerifyPassword(string password, string passwordSalt, string passwordHash)
    {
        return _authRepository.VerifyPassword(password, passwordSalt, passwordHash);
    }
}
