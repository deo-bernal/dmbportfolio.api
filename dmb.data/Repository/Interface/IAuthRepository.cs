using Dmb.Model.Dtos;

namespace Dmb.Data.Repository.Interface;

public interface IAuthRepository
{
    Task<UserDto?> GetUserRecordAsync(string username, CancellationToken cancellationToken = default);
    Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default);
    Task<UserDto> CreateUserAsync(UserDto user, CancellationToken cancellationToken = default);
    Task<bool> IsJtiRevokedAsync(string jti, CancellationToken cancellationToken = default);
    Task RevokeJtiAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default);
}
