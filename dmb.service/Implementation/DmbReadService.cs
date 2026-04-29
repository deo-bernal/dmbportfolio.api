using Dmb.Data.Repository.Interface;
using Dmb.Model.Dtos;
using Dmb.Service.Interface;

namespace Dmb.Service.Implementation;

public class DmbReadService : IDmbReadService
{
    private readonly IDmbReadRepository _dmbReadRepository;

    public DmbReadService(IDmbReadRepository dmbReadRepository)
    {
        _dmbReadRepository = dmbReadRepository;
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
}
