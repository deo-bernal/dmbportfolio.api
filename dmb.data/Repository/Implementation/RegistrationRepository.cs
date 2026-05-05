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

public class RegistrationRepository : IRegistrationRepository
{
    private const int ActivationTokenLifetimeHours = 48;

    private readonly DmbDbContext _dbContext;
    private readonly IAuthRepository _authRepository;
    private readonly IConfiguration _configuration;
    private readonly IActivationEmailSender _activationEmailSender;
    private readonly ILogger<RegistrationRepository> _logger;

    public RegistrationRepository(
        DmbDbContext dbContext,
        IAuthRepository authRepository,
        IConfiguration configuration,
        IActivationEmailSender activationEmailSender,
        ILogger<RegistrationRepository> logger)
    {
        _dbContext = dbContext;
        _authRepository = authRepository;
        _configuration = configuration;
        _activationEmailSender = activationEmailSender;
        _logger = logger;
    }

    public async Task<RegisterWithActivationOutcome> RegisterWithActivationAsync(
        RegisterDto request,
        CancellationToken cancellationToken = default)
    {
        var begin = await BeginRegistrationCoreAsync(request, cancellationToken);
        if (begin.Status == RegistrationBeginStatus.DuplicateEmail)
        {
            return RegisterWithActivationOutcome.DuplicateEmail;
        }

        var email = request.Email.Trim().ToLowerInvariant();
        var frontendUrl = (_configuration["App:FrontendUrl"] ?? string.Empty).TrimEnd('/');
        if (string.IsNullOrWhiteSpace(frontendUrl))
        {
            throw new InvalidOperationException("App:FrontendUrl is not configured.");
        }

        var activationLink = $"{frontendUrl}/activate-account?token={Uri.EscapeDataString(begin.Token)}";

        try
        {
            await _activationEmailSender.SendAccountActivationEmailAsync(email, activationLink, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send account activation email to {Email}.", email);
            await RemoveAccountActivationTokenAndUserAsync(begin.UserId, begin.Token, cancellationToken);
            return RegisterWithActivationOutcome.ActivationEmailSendFailed;
        }

        return RegisterWithActivationOutcome.Success;
    }

    public async Task<ActivateAccountOutcome> CompleteAccountActivationAsync(
        string? token,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return ActivateAccountOutcome.InvalidOrExpiredToken;
        }

        var normalizedToken = token.Trim();

        var activatedAccountEmail = await _dbContext.AccountActivationTokens
            .AsNoTracking()
            .Where(t => t.Token == normalizedToken)
            .Join(
                _dbContext.Users.AsNoTracking(),
                t => t.UserId,
                u => u.UserId,
                (_, u) => u.Email)
            .FirstOrDefaultAsync(cancellationToken);

        var ok = await TryCompleteAccountActivationAsync(normalizedToken, cancellationToken);
        if (!ok)
        {
            return ActivateAccountOutcome.InvalidOrExpiredToken;
        }

        var monitoringEmail = (_configuration["Email:SmtpUser"] ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(monitoringEmail) && !string.IsNullOrWhiteSpace(activatedAccountEmail))
        {
            try
            {
                await _activationEmailSender.SendActivationMonitoringEmailAsync(
                    monitoringEmail,
                    activatedAccountEmail,
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    ex,
                    "Activation succeeded but monitoring email failed for activated account {ActivatedAccountEmail}.",
                    activatedAccountEmail);
                // Activation already succeeded. Do not fail user flow because monitoring email failed.
            }
        }

        return ActivateAccountOutcome.Success;
    }

    private async Task<RegistrationBeginResult> BeginRegistrationCoreAsync(
        RegisterDto request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        if (await _authRepository.EmailAlreadyRegisteredAsync(email, cancellationToken))
        {
            return new RegistrationBeginResult { Status = RegistrationBeginStatus.DuplicateEmail };
        }

        var user = await _authRepository.CreateRegisteredUserAsync(request, cancellationToken);
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
        var expiresAt = DateTimeOffset.UtcNow.AddHours(ActivationTokenLifetimeHours);

        await AddAccountActivationTokenAsync(user.UserId, token, expiresAt, cancellationToken);

        return new RegistrationBeginResult
        {
            Status = RegistrationBeginStatus.Ready,
            UserId = user.UserId,
            Token = token
        };
    }

    private async Task RemoveAccountActivationTokenAndUserAsync(
        int userId,
        string token,
        CancellationToken cancellationToken = default)
    {
        var activation = await _dbContext.AccountActivationTokens
            .FirstOrDefaultAsync(t => t.Token == token && t.UserId == userId, cancellationToken);
        if (activation is not null)
        {
            _dbContext.AccountActivationTokens.Remove(activation);
        }

        var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
        if (user is not null)
        {
            _dbContext.Users.Remove(user);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task AddAccountActivationTokenAsync(
        int userId,
        string token,
        DateTimeOffset expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        _dbContext.AccountActivationTokens.Add(new AccountActivationToken
        {
            UserId = userId,
            Token = token,
            ExpiresAt = expiresAtUtc,
            IsUsed = false
        });
        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<bool> TryCompleteAccountActivationAsync(string token, CancellationToken cancellationToken = default)
    {
        // Load token metadata without tracking the User graph. Updating User + deleting the token in one
        // SaveChanges with Include(User) can trigger concurrency issues (0 rows affected) on PostgreSQL.
        var activation = await _dbContext.AccountActivationTokens
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Token == token, cancellationToken);

        if (activation is null || activation.IsUsed || activation.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return false;
        }

        var userId = activation.UserId;
        var tokenValue = token;

        // EnableRetryOnFailure requires transactions to run inside CreateExecutionStrategy().ExecuteAsync.
        var success = false;
        await _dbContext.Database.CreateExecutionStrategy().ExecuteAsync(async () =>
        {
            await using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                var updated = await _dbContext.Users
                    .Where(u => u.UserId == userId)
                    .ExecuteUpdateAsync(
                        setters => setters.SetProperty(u => u.Activated, true),
                        cancellationToken);

                if (updated == 0)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return;
                }

                var deleted = await _dbContext.AccountActivationTokens
                    .Where(t => t.Token == tokenValue)
                    .ExecuteDeleteAsync(cancellationToken);

                if (deleted == 0)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return;
                }

                await tx.CommitAsync(cancellationToken);
                success = true;
            }
            catch
            {
                await tx.RollbackAsync(cancellationToken);
                throw;
            }
        });

        return success;
    }
}
