using Dmb.Model.Dtos;
using Dmb.Model.Enums;

namespace Dmb.Service.Interface;

public interface IDmbReadService
{
    Task<MyProfileWorkflowResult> GetMyProfileAsync(string? nameIdentifierClaim, CancellationToken cancellationToken = default);

    Task<UserCompleteDetailsDto?> GetUserCompleteDetailsAsync(int userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserCompleteDetailsDto>> GetUsersCompleteDetailsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<UserDetailsDto>> GetUserDetailsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(CancellationToken cancellationToken = default);
    Task<CreateMyProfileStatus> CreateMyProfileAsync(int userId, UpdateMyProfileDto request, CancellationToken cancellationToken = default);
    Task<bool> UpdateMyProfileAsync(int userId, UpdateMyProfileDto request, CancellationToken cancellationToken = default);
}
