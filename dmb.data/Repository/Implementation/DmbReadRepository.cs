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

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (value is null)
        {
            return null;
        }

        return value.Value.Kind switch
        {
            DateTimeKind.Utc => value.Value,
            DateTimeKind.Local => value.Value.ToUniversalTime(),
            // ASP.NET often binds "YYYY-MM-DD" to Kind=Unspecified; Npgsql requires UTC for timestamptz.
            _ => DateTime.SpecifyKind(value.Value, DateTimeKind.Utc),
        };
    }

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

    public async Task<UserCompleteDetailsDto?> GetPublicProfileAsync(string? username, CancellationToken cancellationToken = default)
    {
        var users = _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserDetails)
            .Include(u => u.Projects)
            .ThenInclude(p => p.ProjectType)
            .Where(u => u.Activated && u.IsViewable);

        if (!string.IsNullOrWhiteSpace(username))
        {
            var normalized = username.Trim().ToLowerInvariant();
            users = users.Where(u => u.Username.ToLower() == normalized);
        }

        var user = await users
            .OrderByDescending(u => u.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return user is null ? null : _mapper.Map<UserCompleteDetailsDto>(user);
    }

    public async Task<ResumeDto?> GetPublicResumeAsync(string? username, CancellationToken cancellationToken = default)
    {
        var users = _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserDetails)
            .Include(u => u.WorkHistories)
            .Include(u => u.Educations)
            .Include(u => u.Affiliations)
            .Where(u => u.Activated && u.IsViewable);

        if (!string.IsNullOrWhiteSpace(username))
        {
            var normalized = username.Trim().ToLowerInvariant();
            users = users.Where(u => u.Username.ToLower() == normalized);
        }

        var user = await users
            .OrderByDescending(u => u.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return null;
        }

        return new ResumeDto
        {
            PersonalInfo = new ResumePersonalInfoDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ContactNo = user.ContactNo,
                Address = user.Address,
                Summary = user.UserDetails?.Description
            },
            WorkHistory = user.WorkHistories
                .OrderByDescending(x => x.FromDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.WorkHistoryId)
                .Select(x => new ResumeWorkHistoryDto
                {
                    WorkHistoryId = x.WorkHistoryId,
                    Company = x.Company,
                    Position = x.Position,
                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    JobDescription = x.JobDescription
                })
                .ToList(),
            Education = user.Educations
                .OrderByDescending(x => x.StartDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.EducationId)
                .Select(x => new ResumeEducationDto
                {
                    EducationId = x.EducationId,
                    School = x.School,
                    Address = x.Address,
                    CourseTaken = x.CourseTaken,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate
                })
                .ToList(),
            Affiliations = user.Affiliations
                .OrderByDescending(x => x.IssueDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.AffiliationId)
                .Select(x => new ResumeAffiliationDto
                {
                    AffiliationId = x.AffiliationId,
                    Organization = x.Organization,
                    Title = x.Title,
                    IssueDate = x.IssueDate,
                    Details = x.Details
                })
                .ToList()
        };
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

    public async Task<ResumeDto?> GetMyResumeAsync(int userId, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .Include(u => u.UserDetails)
            .Include(u => u.WorkHistories)
            .Include(u => u.Educations)
            .Include(u => u.Affiliations)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Activated, cancellationToken);

        if (user is null)
        {
            return null;
        }

        return new ResumeDto
        {
            PersonalInfo = new ResumePersonalInfoDto
            {
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                ContactNo = user.ContactNo,
                Address = user.Address,
                Summary = user.UserDetails?.Description
            },
            WorkHistory = user.WorkHistories
                .OrderByDescending(x => x.FromDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.WorkHistoryId)
                .Select(x => new ResumeWorkHistoryDto
                {
                    WorkHistoryId = x.WorkHistoryId,
                    Company = x.Company,
                    Position = x.Position,
                    FromDate = x.FromDate,
                    ToDate = x.ToDate,
                    JobDescription = x.JobDescription
                })
                .ToList(),
            Education = user.Educations
                .OrderByDescending(x => x.StartDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.EducationId)
                .Select(x => new ResumeEducationDto
                {
                    EducationId = x.EducationId,
                    School = x.School,
                    Address = x.Address,
                    CourseTaken = x.CourseTaken,
                    StartDate = x.StartDate,
                    EndDate = x.EndDate
                })
                .ToList(),
            Affiliations = user.Affiliations
                .OrderByDescending(x => x.IssueDate ?? DateTime.MinValue)
                .ThenByDescending(x => x.AffiliationId)
                .Select(x => new ResumeAffiliationDto
                {
                    AffiliationId = x.AffiliationId,
                    Organization = x.Organization,
                    Title = x.Title,
                    IssueDate = x.IssueDate,
                    Details = x.Details
                })
                .ToList()
        };
    }

    public async Task<bool> UpsertMyResumeAsync(int userId, UpdateResumeDto request, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .Include(u => u.UserDetails)
            .Include(u => u.WorkHistories)
            .Include(u => u.Educations)
            .Include(u => u.Affiliations)
            .FirstOrDefaultAsync(u => u.UserId == userId && u.Activated, cancellationToken);

        if (user is null)
        {
            return false;
        }

        user.FirstName = request.PersonalInfo.FirstName.Trim();
        user.LastName = request.PersonalInfo.LastName.Trim();
        user.Email = request.PersonalInfo.Email.Trim();
        user.ContactNo = request.PersonalInfo.ContactNo?.Trim();
        user.Address = request.PersonalInfo.Address?.Trim();

        if (user.UserDetails is null)
        {
            user.UserDetails = new UserDetails
            {
                UserId = user.UserId,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }
        user.UserDetails.Description = request.PersonalInfo.Summary?.Trim();

        _dbContext.WorkHistories.RemoveRange(user.WorkHistories);
        _dbContext.Educations.RemoveRange(user.Educations);
        _dbContext.Affiliations.RemoveRange(user.Affiliations);

        var workHistories = request.WorkHistory
            .Where(x => !string.IsNullOrWhiteSpace(x.Company) && !string.IsNullOrWhiteSpace(x.Position))
            .Select(x => new WorkHistory
            {
                UserId = user.UserId,
                Company = x.Company.Trim(),
                Position = x.Position.Trim(),
                FromDate = NormalizeUtc(x.FromDate),
                ToDate = NormalizeUtc(x.ToDate),
                JobDescription = x.JobDescription?.Trim(),
                CreatedAt = DateTimeOffset.UtcNow
            })
            .ToList();

        var educationItems = request.Education
            .Where(x => !string.IsNullOrWhiteSpace(x.School))
            .Select(x => new Education
            {
                UserId = user.UserId,
                School = x.School.Trim(),
                Address = x.Address?.Trim(),
                CourseTaken = x.CourseTaken?.Trim(),
                StartDate = NormalizeUtc(x.StartDate),
                EndDate = NormalizeUtc(x.EndDate),
                CreatedAt = DateTimeOffset.UtcNow
            })
            .ToList();

        var affiliationItems = request.Affiliations
            .Where(x => !string.IsNullOrWhiteSpace(x.Organization) && !string.IsNullOrWhiteSpace(x.Title))
            .Select(x => new Affiliation
            {
                UserId = user.UserId,
                Organization = x.Organization.Trim(),
                Title = x.Title.Trim(),
                IssueDate = NormalizeUtc(x.IssueDate),
                Details = x.Details?.Trim() ?? string.Empty,
                CreatedAt = DateTimeOffset.UtcNow
            })
            .ToList();

        if (workHistories.Count > 0)
        {
            await _dbContext.WorkHistories.AddRangeAsync(workHistories, cancellationToken);
        }

        if (educationItems.Count > 0)
        {
            await _dbContext.Educations.AddRangeAsync(educationItems, cancellationToken);
        }

        if (affiliationItems.Count > 0)
        {
            await _dbContext.Affiliations.AddRangeAsync(affiliationItems, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
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
        user.IsViewable = request.IsViewable;

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
