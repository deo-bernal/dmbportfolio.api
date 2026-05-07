using Dmb.Data.Repository.Interface;
using Dmb.Model.Dtos;
using Dmb.Model.Enums;
using Dmb.Service.Interface;

namespace Dmb.Service.Implementation;

public class DmbReadService : IDmbReadService
{
    private readonly IDmbReadRepository _dmbReadRepository;

    public DmbReadService(IDmbReadRepository dmbReadRepository)
    {
        _dmbReadRepository = dmbReadRepository;
    }

    public async Task<MyProfileWorkflowResult> GetMyProfileAsync(
        string? nameIdentifierClaim,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await _dmbReadRepository.GetMyProfileByNameIdentifierAsync(nameIdentifierClaim, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return new MyProfileWorkflowResult { Status = MyProfileWorkflowStatus.Canceled };
        }
    }

    public async Task<UserCompleteDetailsDto?> GetUserCompleteDetailsAsync(int userId, CancellationToken cancellationToken = default)
    {
        return await _dmbReadRepository.GetUserCompleteDetailsAsync(userId, cancellationToken);
    }

    public async Task<IReadOnlyList<UserCompleteDetailsDto>> GetUsersCompleteDetailsAsync(CancellationToken cancellationToken = default)
    {
        return await _dmbReadRepository.GetUsersCompleteDetailsAsync(cancellationToken);
    }

    public Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        return _dmbReadRepository.GetUsersAsync(cancellationToken);
    }

    public Task<IReadOnlyList<UserDetailsDto>> GetUserDetailsAsync(CancellationToken cancellationToken = default)
    {
        return _dmbReadRepository.GetUserDetailsAsync(cancellationToken);
    }

    public Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        return _dmbReadRepository.GetProjectsAsync(cancellationToken);
    }

    public Task<CreateMyProfileStatus> CreateMyProfileAsync(int userId, UpdateMyProfileDto request, CancellationToken cancellationToken = default)
    {
        return _dmbReadRepository.CreateMyProfileAsync(userId, request, cancellationToken);
    }

    public Task<bool> UpdateMyProfileAsync(int userId, UpdateMyProfileDto request, CancellationToken cancellationToken = default)
    {
        return _dmbReadRepository.UpdateMyProfileAsync(userId, request, cancellationToken);
    }
}
