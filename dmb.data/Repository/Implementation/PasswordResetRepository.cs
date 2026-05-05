using System.Security.Cryptography;
using Dmb.Data.Context;
using Dmb.Data.Entities;
using Dmb.Data.Repository.Interface;
using Dmb.Model.Abstractions;
using Dmb.Model.Dtos;
using Dmb.Model.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dmb.Data.Repository.Implementation;

public class PasswordResetRepository : IPasswordResetRepository
{
    private readonly DmbDbContext _dbContext;
    private readonly IAuthRepository _authRepository;
    private readonly IConfiguration _configuration;
    private readonly IPasswordResetEmailSender _passwordResetEmailSender;
    private readonly ILogger<PasswordResetRepository> _logger;

    public PasswordResetRepository(
        DmbDbContext dbContext,
        IAuthRepository authRepository,
        IConfiguration configuration,
        IPasswordResetEmailSender passwordResetEmailSender,
        ILogger<PasswordResetRepository> logger)
    {
        _dbContext = dbContext;
        _authRepository = authRepository;
        _configuration = configuration;
        _passwordResetEmailSender = passwordResetEmailSender;
        _logger = logger;
    }

    public async Task<ForgotPasswordRequestStatus> RequestPasswordResetAsync(
        ForgotPasswordDto request,
        CancellationToken cancellationToken = default)
    {
        var normalized = request.Email.Trim().ToLowerInvariant();
        var user = await _dbContext.Users
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalized, cancellationToken);

        if (user is null)
        {
            return ForgotPasswordRequestStatus.Ok;
        }

        var existingTokens = await _dbContext.PasswordResetTokens
            .Where(t => t.UserId == user.UserId)
            .ToListAsync(cancellationToken);
        if (existingTokens.Count > 0)
        {
            _dbContext.PasswordResetTokens.RemoveRange(existingTokens);
        }

        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var resetToken = new PasswordResetToken
        {
            UserId = user.UserId,
            Token = token,
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            IsUsed = false
        };

        _dbContext.PasswordResetTokens.Add(resetToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var frontendUrl = (_configuration["App:FrontendUrl"] ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            throw new InvalidOperationException("App:FrontendUrl is not configured.");
        }

        var resetLink = $"{frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";

        try
        {
            await _passwordResetEmailSender.SendPasswordResetEmailAsync(user.Email, resetLink, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}.", user.Email);
            _dbContext.PasswordResetTokens.Remove(resetToken);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return ForgotPasswordRequestStatus.EmailServiceUnavailable;
        }

        return ForgotPasswordRequestStatus.Ok;
    }

    public async Task<PasswordResetCompletionStatus> CompletePasswordResetAsync(
        ResetPasswordDto request,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.NewPassword, request.ConfirmPassword, StringComparison.Ordinal))
        {
            return PasswordResetCompletionStatus.PasswordMismatch;
        }

        var resetToken = await _dbContext.PasswordResetTokens
            .Include(t => t.User)
            .FirstOrDefaultAsync(t => t.Token == request.Token, cancellationToken);

        if (resetToken is null || resetToken.IsUsed || resetToken.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return PasswordResetCompletionStatus.InvalidOrExpiredToken;
        }

        var (passwordHash, passwordSalt) = _authRepository.CreatePasswordHash(request.NewPassword);
        resetToken.User.PasswordHash = passwordHash;
        resetToken.User.PasswordSalt = passwordSalt;

        _dbContext.PasswordResetTokens.Remove(resetToken);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return PasswordResetCompletionStatus.Success;
    }
}
