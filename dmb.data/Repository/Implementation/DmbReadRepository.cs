using AutoMapper;
using Dmb.Data.Context;
using Dmb.Data.Repository.Interface;
using Dmb.Model.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Dmb.Data.Repository.Implementation;

public class DmbReadRepository : IDmbReadRepository
{
    private readonly DmbDbContext _dbContext;
    private readonly IMapper _mapper;

    public DmbReadRepository(DmbDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<UserCompleteDetailsDto?> GetUserCompleteDetailsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserDetails)
            .Include(u => u.Projects)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Activated, cancellationToken);

        return user is null ? null : _mapper.Map<UserCompleteDetailsDto>(user);
    }

    public async Task<IReadOnlyList<UserCompleteDetailsDto>> GetUsersCompleteDetailsAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserDetails)
            .Include(u => u.Projects)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<UserCompleteDetailsDto>>(users);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserDetails)
            .Include(u => u.Projects)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<UserDto>>(users);
    }

    public async Task<IReadOnlyList<UserDetailsDto>> GetUserDetailsAsync(CancellationToken cancellationToken = default)
    {
        var userDetails = await _dbContext.UserDetails
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<UserDetailsDto>>(userDetails);
    }

    public async Task<IReadOnlyList<ProjectDto>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        var projects = await _dbContext.Projects
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<ProjectDto>>(projects);
    }
}
