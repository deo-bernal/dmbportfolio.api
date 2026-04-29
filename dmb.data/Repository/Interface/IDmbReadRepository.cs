using Dmb.Model.Dtos;

namespace Dmb.Data.Repository.Interface;

public interface IDmbReadRepository
{
    Task<UserCompleteDetailsDto?> GetUserCompleteDetailsAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserCompleteDetailsDto>> GetUsersCompleteDetailsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDetailsDto>> GetUserDetailsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(CancellationToken cancellationToken = default);
}
