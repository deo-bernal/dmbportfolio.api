using AutoMapper;
using Dmb.Data.Context;
using Dmb.Data.Repository.Interface;
using Dmb.Model.Dtos;
using Dmb.Model.Enums;
using Dmb.Data.Entities;
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

    public async Task<MyProfileWorkflowResult> GetMyProfileByNameIdentifierAsync(
        string? nameIdentifier,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(nameIdentifier) || !int.TryParse(nameIdentifier.Trim(), out var userId))
        {
            return new MyProfileWorkflowResult { Status = MyProfileWorkflowStatus.InvalidUserContext };
        }

        var details = await GetUserCompleteDetailsAsync(userId, cancellationToken);
        if (details is null)
        {
            return new MyProfileWorkflowResult { Status = MyProfileWorkflowStatus.NotFound };
        }

        return new MyProfileWorkflowResult { Status = MyProfileWorkflowStatus.Ok, Details = details };
    }

    public async Task<UserCompleteDetailsDto?> GetUserCompleteDetailsAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserDetails)
            .Include(u => u.Projects)
            .ThenInclude(p => p.ProjectType)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Activated, cancellationToken);

        return user is null ? null : _mapper.Map<UserCompleteDetailsDto>(user);
    }

    public async Task<IReadOnlyList<UserCompleteDetailsDto>> GetUsersCompleteDetailsAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserDetails)
            .Include(u => u.Projects)
            .ThenInclude(p => p.ProjectType)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<UserCompleteDetailsDto>>(users);
    }

    public async Task<IReadOnlyList<UserDto>> GetUsersAsync(CancellationToken cancellationToken = default)
    {
        var users = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserDetails)
            .Include(u => u.Projects)
            .ThenInclude(p => p.ProjectType)
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
            .Include(p => p.ProjectType)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IReadOnlyList<ProjectDto>>(projects);
    }

    public async Task<CreateMyProfileStatus> CreateMyProfileAsync(int userId, UpdateMyProfileDto request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserDetails)
            .Include(u => u.Projects)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Activated, cancellationToken);

        if (user is null)
        {
            return CreateMyProfileStatus.NotFound;
        }

        if (user.UserDetails is not null || user.Projects.Count > 0)
        {
            return CreateMyProfileStatus.AlreadyExists;
        }

        await ApplyProfileChangesAsync(user, request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return CreateMyProfileStatus.Created;
    }

    public async Task<bool> UpdateMyProfileAsync(int userId, UpdateMyProfileDto request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserDetails)
            .Include(u => u.Projects)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Activated, cancellationToken);

        if (user is null)
        {
            return false;
        }

        await ApplyProfileChangesAsync(user, request, cancellationToken);
        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    private async Task ApplyProfileChangesAsync(User user, UpdateMyProfileDto request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(request.Contact.Email))
        {
            user.Email = request.Contact.Email.Trim();
        }

        user.ContactNo = request.Contact.Phone?.Trim();

        if (user.UserDetails is null)
        {
            user.UserDetails = new UserDetails
            {
                UserId = user.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        user.UserDetails.Description = request.Summary?.Trim();
        user.UserDetails.Video = request.Video?.Trim();
        user.UserDetails.Skills = string.Join(",", request.Skills.Where(s => !string.IsNullOrWhiteSpace(s)).Select(s => s.Trim()));

        _dbContext.Projects.RemoveRange(user.Projects);

        var normalizedTitles = request.ProjectCategories
            .Where(c => !string.IsNullOrWhiteSpace(c.Title))
            .Select(c => c.Title.Trim())
            .Distinct()
            .ToList();

        var typeNameLookup = await _dbContext.ProjectTypes
            .Where(pt => normalizedTitles.Contains(pt.TypeName))
            .ToDictionaryAsync(pt => pt.TypeName, pt => pt.ProjectTypeId, cancellationToken);

        var missingTitles = normalizedTitles.Where(title => !typeNameLookup.ContainsKey(title)).ToList();
        if (missingTitles.Count > 0)
        {
            var projectTypesToAdd = missingTitles.Select(title => new ProjectType
            {
                TypeName = title,
                CreatedAt = DateTimeOffset.UtcNow
            });
            await _dbContext.ProjectTypes.AddRangeAsync(projectTypesToAdd, cancellationToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            var createdTypes = await _dbContext.ProjectTypes
                .Where(pt => missingTitles.Contains(pt.TypeName))
                .ToListAsync(cancellationToken);
            foreach (var projectType in createdTypes)
            {
                typeNameLookup[projectType.TypeName] = projectType.ProjectTypeId;
            }
        }

        var projectsToAdd = new List<Project>();
        foreach (var category in request.ProjectCategories.Where(c => !string.IsNullOrWhiteSpace(c.Title)))
        {
            var normalizedTitle = category.Title.Trim();
            if (!typeNameLookup.TryGetValue(normalizedTitle, out var projectTypeId))
            {
                continue;
            }

            foreach (var item in category.Items.Where(i => !string.IsNullOrWhiteSpace(i.Name)))
            {
                projectsToAdd.Add(new Project
                {
                    UserId = user.UserId,
                    ProjectTypeId = projectTypeId,
                    Name = item.Name.Trim(),
                    ProjectDetails = item.Description?.Trim(),
                    CreatedAt = DateTimeOffset.UtcNow
                });
            }
        }

        if (projectsToAdd.Count > 0)
        {
            await _dbContext.Projects.AddRangeAsync(projectsToAdd, cancellationToken);
        }
    }
}
