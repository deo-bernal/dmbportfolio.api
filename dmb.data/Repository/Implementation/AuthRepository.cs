using AutoMapper;
using Dmb.Data.Context;
using Dmb.Data.Entities;
using Dmb.Data.Repository.Interface;
using Dmb.Model.Dtos;
using Microsoft.EntityFrameworkCore;

namespace Dmb.Data.Repository.Implementation;

public class AuthRepository : IAuthRepository
{
    private readonly DmbDbContext _dbContext;
    private readonly IMapper _mapper;

    public AuthRepository(DmbDbContext dbContext, IMapper mapper)
    {
        _dbContext = dbContext;
        _mapper = mapper;
    }

    public async Task<UserDto?> GetUserRecordAsync(string username, CancellationToken cancellationToken = default)
    {
        var user = await _dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(
                user => user.Username == username && user.Activated,
                cancellationToken);

        return user is null ? null : _mapper.Map<UserDto>(user);
    }

    public Task<bool> UsernameExistsAsync(string username, CancellationToken cancellationToken = default)
    {
        return _dbContext.Users
            .AsNoTracking()
            .AnyAsync(user => user.Username == username, cancellationToken);
    }

    public async Task<UserDto> CreateUserAsync(UserDto user, CancellationToken cancellationToken = default)
    {
        var entity = new User
        {
            Username = user.Username,
            PasswordHash = user.PasswordHash,
            PasswordSalt = user.PasswordSalt,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Email = user.Email,
            ContactNo = user.ContactNo,
            Activated = user.Activated,
            CreatedAt = user.CreatedAt
        };

        _dbContext.Users.Add(entity);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return _mapper.Map<UserDto>(entity);
    }

    public Task<bool> IsJtiRevokedAsync(string jti, CancellationToken cancellationToken = default)
    {
        return _dbContext.RevokedTokens
            .AsNoTracking()
            .AnyAsync(revokedToken => revokedToken.Jti == jti, cancellationToken);
    }

    public async Task RevokeJtiAsync(string jti, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        var alreadyRevoked = await IsJtiRevokedAsync(jti, cancellationToken);
        if (alreadyRevoked)
        {
            return;
        }

        _dbContext.RevokedTokens.Add(new RevokedToken
        {
            Jti = jti,
            ExpiresAt = expiresAt,
            CreatedAt = DateTimeOffset.UtcNow
        });

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
