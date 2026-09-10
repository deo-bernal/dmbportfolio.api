using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Dmb.Data.Context;
using Dmb.Data.Entities;
using Dmb.Data.Repository.Interface;
using Dmb.Model.Dtos;
using Dmb.Model.Enums;
using Dmb.Service.Interface;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Dmb.Service.Implementation;

public class ExternalAuthService : IExternalAuthService
{
    private static readonly HashSet<string> Providers = new(StringComparer.OrdinalIgnoreCase)
    {
        "google", "linkedin", "facebook"
    };

    private readonly DmbDbContext _db;
    private readonly IAuthRepository _authRepository;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExternalAuthService> _logger;

    public ExternalAuthService(
        DmbDbContext db,
        IAuthRepository authRepository,
        IEmailService emailService,
        IConfiguration configuration,
        HttpClient httpClient,
        ILogger<ExternalAuthService> logger)
    {
        _db = db;
        _authRepository = authRepository;
        _emailService = emailService;
        _configuration = configuration;
        _httpClient = httpClient;
        _logger = logger;
    }

    public ExternalAuthStartResult Start(string provider, string? client, string? redirect, string callbackUrl)
    {
        var normalized = NormalizeProvider(provider);
        if (normalized is null)
        {
            return new ExternalAuthStartResult { ErrorMessage = "Unknown sign-in provider." };
        }

        if (!TryGetProviderConfig(normalized, out var clientId, out _))
        {
            return new ExternalAuthStartResult
            {
                RedirectUrl = ErrorRedirect(client, redirect, $"{Title(normalized)} sign-in is not configured yet.")
            };
        }

        var clientKind = NormalizeClient(client);
        var state = CreateState(clientKind, redirect);
        var authUrl = normalized switch
        {
            "google" =>
                "https://accounts.google.com/o/oauth2/v2/auth"
                + "?response_type=code"
                + "&scope=" + Uri.EscapeDataString("openid email profile")
                + "&client_id=" + Uri.EscapeDataString(clientId)
                + "&redirect_uri=" + Uri.EscapeDataString(callbackUrl)
                + "&state=" + Uri.EscapeDataString(state)
                + "&access_type=online"
                + "&prompt=select_account",
            "linkedin" =>
                "https://www.linkedin.com/oauth/v2/authorization"
                + "?response_type=code"
                + "&scope=" + Uri.EscapeDataString("openid profile email")
                + "&client_id=" + Uri.EscapeDataString(clientId)
                + "&redirect_uri=" + Uri.EscapeDataString(callbackUrl)
                + "&state=" + Uri.EscapeDataString(state),
            _ =>
                "https://www.facebook.com/v21.0/dialog/oauth"
                + "?response_type=code"
                + "&scope=" + Uri.EscapeDataString("email")
                + "&client_id=" + Uri.EscapeDataString(clientId)
                + "&redirect_uri=" + Uri.EscapeDataString(callbackUrl)
                + "&state=" + Uri.EscapeDataString(state)
        };

        return new ExternalAuthStartResult { RedirectUrl = authUrl };
    }

    public async Task<string> HandleCallbackAsync(
        string provider,
        string? code,
        string? state,
        string? error,
        string? errorDescription,
        string callbackUrl,
        CancellationToken cancellationToken = default)
    {
        var normalized = NormalizeProvider(provider);
        var parsedState = ParseState(state);
        var client = parsedState?.Client ?? "web";
        var returnPath = parsedState?.ReturnPath;

        if (normalized is null)
        {
            return ErrorRedirect(client, returnPath, "Unknown sign-in provider.");
        }

        if (!string.IsNullOrWhiteSpace(error))
        {
            _logger.LogWarning(
                "OAuth provider {Provider} returned error {Error}: {Description}",
                normalized,
                error,
                errorDescription);
            return ErrorRedirect(client, returnPath, DescribeProviderError(normalized, error, errorDescription));
        }

        if (parsedState is null)
        {
            return ErrorRedirect(client, returnPath, "Sign-in session expired. Try again.");
        }

        if (string.IsNullOrWhiteSpace(code))
        {
            return ErrorRedirect(client, returnPath, $"{Title(normalized)} did not return an authorization code.");
        }

        if (!TryGetProviderConfig(normalized, out var clientId, out var clientSecret))
        {
            return ErrorRedirect(client, returnPath, $"{Title(normalized)} sign-in is not configured yet.");
        }

        OAuthProfile profile;
        try
        {
            profile = await ExchangeCodeAsync(normalized, code, callbackUrl, clientId, clientSecret, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "OAuth token exchange failed for {Provider}.", normalized);
            return ErrorRedirect(client, returnPath, $"Could not complete {Title(normalized)} sign-in. Try again.");
        }

        if (string.IsNullOrWhiteSpace(profile.ProviderUserId))
        {
            return ErrorRedirect(client, returnPath, $"{Title(normalized)} did not return a user id.");
        }

        var existingLink = await _db.ExternalLogins
            .AsNoTracking()
            .FirstOrDefaultAsync(
                l => l.Provider == normalized && l.ProviderUserId == profile.ProviderUserId,
                cancellationToken);

        if (existingLink is not null)
        {
            return await IssueAndRedirectAsync(existingLink.UserId, client, returnPath, cancellationToken);
        }

        if (!string.IsNullOrWhiteSpace(profile.Email))
        {
            var email = profile.Email.Trim().ToLowerInvariant();
            var user = await _db.Users.FirstOrDefaultAsync(
                u => u.Email.ToLower() == email || u.Username.ToLower() == email,
                cancellationToken);

            if (user is not null)
            {
                await AttachExternalLoginAsync(user.UserId, normalized, profile.ProviderUserId, cancellationToken);
                if (!user.Activated)
                {
                    user.Activated = true;
                    await _db.SaveChangesAsync(cancellationToken);
                }

                return await IssueAndRedirectAsync(user.UserId, client, returnPath, cancellationToken);
            }

            var created = await CreateSocialUserAsync(profile, email, cancellationToken);
            await AttachExternalLoginAsync(created.UserId, normalized, profile.ProviderUserId, cancellationToken);
            return await IssueAndRedirectAsync(created.UserId, client, returnPath, cancellationToken);
        }

        var ticket = CreateTicket();
        _db.PendingExternalLogins.Add(new PendingExternalLogin
        {
            Ticket = ticket,
            Provider = normalized,
            ProviderUserId = profile.ProviderUserId,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            Phone = ClipPhone(profile.Phone),
            Client = client,
            ReturnPath = returnPath,
            ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15),
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        return CompleteRedirect(client, returnPath, ticket);
    }

    public async Task<(bool Ok, string Message, int StatusCode)> CompleteAsync(
        ExternalAuthCompleteRequest request,
        CancellationToken cancellationToken = default)
    {
        var pending = await GetValidPendingAsync(request.Ticket, cancellationToken);
        if (pending is null)
        {
            return (false, "This sign-up session expired. Start again with the social button.", 400);
        }

        var email = request.Email?.Trim().ToLowerInvariant() ?? "";
        if (email.Length < 5 || !email.Contains('@', StringComparison.Ordinal))
        {
            return (false, "Enter a valid email address.", 400);
        }

        var phone = ClipPhone(request.Phone) ?? pending.Phone;
        var code = RandomNumberGenerator.GetInt32(0, 1_000_000).ToString("D6");

        pending.Email = email;
        pending.Phone = phone;
        pending.CodeHash = HashToken(code);
        pending.CodeExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        pending.ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(15);
        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _emailService.SendExternalLoginCodeEmailAsync(email, code, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send SSO verification code to {Email}.", email);
            return (false, "Unable to send the verification email. Please try again later.", 503);
        }

        return (true, "We sent a 6-digit code to that email.", 200);
    }

    public async Task<ExternalAuthVerifyResult> VerifyAsync(
        ExternalAuthVerifyRequest request,
        CancellationToken cancellationToken = default)
    {
        var pending = await GetValidPendingAsync(request.Ticket, cancellationToken);
        if (pending is null)
        {
            return Fail("This sign-up session expired. Start again with the social button.", 400);
        }

        if (string.IsNullOrWhiteSpace(pending.Email)
            || string.IsNullOrWhiteSpace(pending.CodeHash)
            || pending.CodeExpiresAt is null
            || pending.CodeExpiresAt < DateTimeOffset.UtcNow)
        {
            return Fail("Request a new code first.", 400);
        }

        var submitted = (request.Code ?? "").Trim();
        if (submitted.Length != 6 || HashToken(submitted) != pending.CodeHash)
        {
            return Fail("That code is incorrect.", 400);
        }

        var email = pending.Email.Trim().ToLowerInvariant();
        var user = await _db.Users.FirstOrDefaultAsync(
            u => u.Email.ToLower() == email || u.Username.ToLower() == email,
            cancellationToken);

        if (user is null)
        {
            var created = await CreateSocialUserAsync(
                new OAuthProfile
                {
                    ProviderUserId = pending.ProviderUserId,
                    Email = email,
                    FirstName = pending.FirstName,
                    LastName = pending.LastName,
                    Phone = pending.Phone
                },
                email,
                cancellationToken);
            user = await _db.Users.FirstAsync(u => u.UserId == created.UserId, cancellationToken);
        }
        else if (!user.Activated)
        {
            user.Activated = true;
        }

        if (!string.IsNullOrWhiteSpace(pending.Phone) && string.IsNullOrWhiteSpace(user.ContactNo))
        {
            user.ContactNo = ClipPhone(pending.Phone);
        }

        await AttachExternalLoginAsync(user.UserId, pending.Provider, pending.ProviderUserId, cancellationToken);
        _db.PendingExternalLogins.Remove(pending);
        await _db.SaveChangesAsync(cancellationToken);

        var tokens = string.Equals(pending.Client, "app", StringComparison.OrdinalIgnoreCase)
            ? await _authRepository.IssueAppTokensForUserAsync(user.UserId, cancellationToken)
            : await _authRepository.IssueJwtForUserAsync(user.UserId, cancellationToken);

        if (tokens.Status != AuthTokenLoginStatus.Success)
        {
            return Fail(tokens.BlockReason ?? "Could not sign you in.", 403);
        }

        return new ExternalAuthVerifyResult
        {
            Success = true,
            Client = pending.Client,
            AccessToken = tokens.AccessToken,
            RefreshToken = tokens.RefreshToken,
            IsPinSet = tokens.IsPinSet
        };
    }

    private async Task<string> IssueAndRedirectAsync(
        int userId,
        string client,
        string? returnPath,
        CancellationToken cancellationToken)
    {
        if (string.Equals(client, "app", StringComparison.OrdinalIgnoreCase))
        {
            var tokens = await _authRepository.IssueAppTokensForUserAsync(userId, cancellationToken);
            if (tokens.Status != AuthTokenLoginStatus.Success || string.IsNullOrWhiteSpace(tokens.AccessToken))
            {
                return ErrorRedirect(client, returnPath, tokens.BlockReason ?? "Could not sign you in.");
            }

            var appBase = AppCallbackBase(returnPath);
            return appBase
                + (appBase.Contains('?', StringComparison.Ordinal) ? "&" : "?")
                + "accessToken=" + Uri.EscapeDataString(tokens.AccessToken)
                + "&refreshToken=" + Uri.EscapeDataString(tokens.RefreshToken ?? "")
                + "&isPinSet=" + (tokens.IsPinSet ? "true" : "false");
        }

        var jwt = await _authRepository.IssueJwtForUserAsync(userId, cancellationToken);
        if (jwt.Status != AuthTokenLoginStatus.Success || string.IsNullOrWhiteSpace(jwt.AccessToken))
        {
            return ErrorRedirect(client, returnPath, jwt.BlockReason ?? "Could not sign you in.");
        }

        var webPath = GetSafeRedirectPath(returnPath);
        var url = FrontendUrl().TrimEnd('/') + "/auth/callback?token=" + Uri.EscapeDataString(jwt.AccessToken);
        if (!string.IsNullOrWhiteSpace(webPath))
        {
            url += "&redirect=" + Uri.EscapeDataString(webPath);
        }

        return url;
    }

    private async Task AttachExternalLoginAsync(
        int userId,
        string provider,
        string providerUserId,
        CancellationToken cancellationToken)
    {
        var exists = await _db.ExternalLogins.AnyAsync(
            l => l.Provider == provider && l.ProviderUserId == providerUserId,
            cancellationToken);
        if (exists)
        {
            return;
        }

        var sameProvider = await _db.ExternalLogins.AnyAsync(
            l => l.UserId == userId && l.Provider == provider,
            cancellationToken);
        if (sameProvider)
        {
            return;
        }

        _db.ExternalLogins.Add(new ExternalLogin
        {
            UserId = userId,
            Provider = provider,
            ProviderUserId = providerUserId,
            CreatedAt = DateTimeOffset.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<UserDto> CreateSocialUserAsync(
        OAuthProfile profile,
        string email,
        CancellationToken cancellationToken)
    {
        var randomPassword = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var (hash, salt) = _authRepository.CreatePasswordHash(randomPassword);
        return await _authRepository.CreateUserAsync(new UserDto
        {
            Username = email,
            Email = email,
            FirstName = profile.FirstName,
            LastName = profile.LastName,
            ContactNo = ClipPhone(profile.Phone),
            PasswordHash = hash,
            PasswordSalt = salt,
            Activated = true,
            PasswordSet = false,
            CreatedAt = DateTimeOffset.UtcNow
        }, cancellationToken);
    }

    private async Task<PendingExternalLogin?> GetValidPendingAsync(string? ticket, CancellationToken cancellationToken)
    {
        var value = ticket?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var pending = await _db.PendingExternalLogins
            .FirstOrDefaultAsync(p => p.Ticket == value, cancellationToken);
        if (pending is null || pending.ExpiresAt < DateTimeOffset.UtcNow)
        {
            return null;
        }

        return pending;
    }

    private async Task<OAuthProfile> ExchangeCodeAsync(
        string provider,
        string code,
        string callbackUrl,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        return provider switch
        {
            "google" => await ExchangeGoogleAsync(code, callbackUrl, clientId, clientSecret, cancellationToken),
            "linkedin" => await ExchangeLinkedInAsync(code, callbackUrl, clientId, clientSecret, cancellationToken),
            _ => await ExchangeFacebookAsync(code, callbackUrl, clientId, clientSecret, cancellationToken)
        };
    }

    private async Task<OAuthProfile> ExchangeGoogleAsync(
        string code,
        string callbackUrl,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        using var tokenResponse = await _httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["code"] = code,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret,
                ["redirect_uri"] = callbackUrl,
                ["grant_type"] = "authorization_code"
            }),
            cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();
        var token = await tokenResponse.Content.ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Google token response was empty.");

        using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v3/userinfo");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var userResponse = await _httpClient.SendAsync(userRequest, cancellationToken);
        userResponse.EnsureSuccessStatusCode();
        var user = await userResponse.Content.ReadFromJsonAsync<GoogleUserInfo>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Google profile was empty.");

        var (first, last) = SplitName(user.GivenName, user.FamilyName, user.Name);
        return new OAuthProfile
        {
            ProviderUserId = user.Sub ?? "",
            Email = string.IsNullOrWhiteSpace(user.Email) || user.EmailVerified == false ? null : user.Email,
            FirstName = first,
            LastName = last
        };
    }

    private async Task<OAuthProfile> ExchangeLinkedInAsync(
        string code,
        string callbackUrl,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        using var tokenResponse = await _httpClient.PostAsync(
            "https://www.linkedin.com/oauth/v2/accessToken",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "authorization_code",
                ["code"] = code,
                ["redirect_uri"] = callbackUrl,
                ["client_id"] = clientId,
                ["client_secret"] = clientSecret
            }),
            cancellationToken);
        if (!tokenResponse.IsSuccessStatusCode)
        {
            var body = await tokenResponse.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogWarning("LinkedIn token exchange failed ({Status}): {Body}", tokenResponse.StatusCode, body);
            throw new InvalidOperationException("LinkedIn token exchange failed.");
        }

        var token = await tokenResponse.Content.ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("LinkedIn token response was empty.");

        using var userRequest = new HttpRequestMessage(HttpMethod.Get, "https://api.linkedin.com/v2/userinfo");
        userRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.AccessToken);
        using var userResponse = await _httpClient.SendAsync(userRequest, cancellationToken);
        userResponse.EnsureSuccessStatusCode();
        var user = await userResponse.Content.ReadFromJsonAsync<LinkedInUserInfo>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("LinkedIn profile was empty.");

        var (first, last) = SplitName(user.GivenName, user.FamilyName, user.Name);
        return new OAuthProfile
        {
            ProviderUserId = user.Sub ?? "",
            Email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email,
            FirstName = first,
            LastName = last
        };
    }

    private async Task<OAuthProfile> ExchangeFacebookAsync(
        string code,
        string callbackUrl,
        string clientId,
        string clientSecret,
        CancellationToken cancellationToken)
    {
        var tokenUrl =
            "https://graph.facebook.com/v21.0/oauth/access_token"
            + "?client_id=" + Uri.EscapeDataString(clientId)
            + "&redirect_uri=" + Uri.EscapeDataString(callbackUrl)
            + "&client_secret=" + Uri.EscapeDataString(clientSecret)
            + "&code=" + Uri.EscapeDataString(code);

        using var tokenResponse = await _httpClient.GetAsync(tokenUrl, cancellationToken);
        tokenResponse.EnsureSuccessStatusCode();
        var token = await tokenResponse.Content.ReadFromJsonAsync<OAuthTokenResponse>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Facebook token response was empty.");

        var profileUrl =
            "https://graph.facebook.com/me"
            + "?fields=id,first_name,last_name,name,email"
            + "&access_token=" + Uri.EscapeDataString(token.AccessToken ?? "");
        using var userResponse = await _httpClient.GetAsync(profileUrl, cancellationToken);
        userResponse.EnsureSuccessStatusCode();
        var user = await userResponse.Content.ReadFromJsonAsync<FacebookUserInfo>(cancellationToken: cancellationToken)
            ?? throw new InvalidOperationException("Facebook profile was empty.");

        var (first, last) = SplitName(user.FirstName, user.LastName, user.Name);
        return new OAuthProfile
        {
            ProviderUserId = user.Id ?? "",
            Email = string.IsNullOrWhiteSpace(user.Email) ? null : user.Email,
            FirstName = first,
            LastName = last
        };
    }

    private bool TryGetProviderConfig(string provider, out string clientId, out string clientSecret)
    {
        var section = provider switch
        {
            "google" => "Authentication:Google",
            "linkedin" => "Authentication:LinkedIn",
            _ => "Authentication:Facebook"
        };
        clientId = (_configuration[$"{section}:ClientId"] ?? "").Trim();
        clientSecret = (_configuration[$"{section}:ClientSecret"] ?? "").Trim();
        return clientId.Length > 0 && clientSecret.Length > 0;
    }

    private string CreateState(string client, string? redirect)
    {
        var payload = JsonSerializer.Serialize(new OAuthStatePayload
        {
            Nonce = Guid.NewGuid().ToString("N"),
            Client = client,
            ReturnPath = string.Equals(client, "app", StringComparison.OrdinalIgnoreCase)
                ? SanitizeAppReturn(redirect)
                : GetSafeRedirectPath(redirect),
            Exp = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds()
        });
        var payloadBytes = Encoding.UTF8.GetBytes(payload);
        var signature = HMACSHA256.HashData(StateKey(), payloadBytes);
        return ToBase64Url(payloadBytes) + "." + ToBase64Url(signature);
    }

    private OAuthStatePayload? ParseState(string? state)
    {
        if (string.IsNullOrWhiteSpace(state))
        {
            return null;
        }

        var parts = state.Split('.', 2);
        if (parts.Length != 2)
        {
            return null;
        }

        try
        {
            var payloadBytes = FromBase64Url(parts[0]);
            var signature = FromBase64Url(parts[1]);
            var expected = HMACSHA256.HashData(StateKey(), payloadBytes);
            if (!CryptographicOperations.FixedTimeEquals(signature, expected))
            {
                return null;
            }

            var payload = JsonSerializer.Deserialize<OAuthStatePayload>(payloadBytes);
            if (payload is null || payload.Exp < DateTimeOffset.UtcNow.ToUnixTimeSeconds())
            {
                return null;
            }

            payload.Client = NormalizeClient(payload.Client);
            return payload;
        }
        catch
        {
            return null;
        }
    }

    private byte[] StateKey()
    {
        var secret = _configuration["Jwt:Secret"]
            ?? throw new InvalidOperationException("Jwt:Secret is not configured.");
        return Encoding.UTF8.GetBytes(secret);
    }

    private string FrontendUrl()
    {
        var url = (_configuration["App:FrontendUrl"] ?? "http://localhost:3000").Trim();
        return url.TrimEnd('/');
    }

    private string ErrorRedirect(string? client, string? returnPath, string message)
    {
        if (string.Equals(NormalizeClient(client), "app", StringComparison.OrdinalIgnoreCase))
        {
            var appBase = AppCallbackBase(returnPath);
            return appBase
                + (appBase.Contains('?', StringComparison.Ordinal) ? "&" : "?")
                + "error=" + Uri.EscapeDataString(message);
        }

        var login = FrontendUrl() + "/login?ssoError=" + Uri.EscapeDataString(message);
        var path = GetSafeRedirectPath(returnPath);
        if (!string.IsNullOrWhiteSpace(path))
        {
            login += "&redirect=" + Uri.EscapeDataString(path);
        }

        return login;
    }

    private string CompleteRedirect(string client, string? returnPath, string ticket)
    {
        if (string.Equals(client, "app", StringComparison.OrdinalIgnoreCase))
        {
            var complete = AppCompleteBase(returnPath);
            return complete
                + (complete.Contains('?', StringComparison.Ordinal) ? "&" : "?")
                + "ticket=" + Uri.EscapeDataString(ticket);
        }

        return FrontendUrl() + "/auth/complete?ticket=" + Uri.EscapeDataString(ticket);
    }

    private static string AppCallbackBase(string? returnPath)
    {
        var sanitized = SanitizeAppReturn(returnPath);
        return string.IsNullOrWhiteSpace(sanitized) ? "dmbportfolio://auth/callback" : sanitized;
    }

    private static string AppCompleteBase(string? returnPath)
    {
        var callback = AppCallbackBase(returnPath);
        return callback.Replace("auth/callback", "auth/complete", StringComparison.OrdinalIgnoreCase);
    }

    private static string NormalizeClient(string? client)
    {
        return string.Equals(client, "app", StringComparison.OrdinalIgnoreCase) ? "app" : "web";
    }

    private static string? NormalizeProvider(string? provider)
    {
        var value = provider?.Trim().ToLowerInvariant();
        return value is not null && Providers.Contains(value) ? value : null;
    }

    private static string Title(string provider) => provider switch
    {
        "google" => "Google",
        "linkedin" => "LinkedIn",
        "facebook" => "Facebook",
        _ => provider
    };

    private static string DescribeProviderError(string provider, string error, string? errorDescription)
    {
        if (string.Equals(error, "access_denied", StringComparison.OrdinalIgnoreCase))
        {
            return $"{Title(provider)} sign-in was cancelled.";
        }

        if (string.Equals(error, "unauthorized_scope_error", StringComparison.OrdinalIgnoreCase)
            || (errorDescription?.Contains("scope", StringComparison.OrdinalIgnoreCase) ?? false))
        {
            return "LinkedIn did not allow openid/profile/email. On the LinkedIn app, add the product \"Sign In with LinkedIn using OpenID Connect\", confirm those scopes on the Auth tab, then try again.";
        }

        var detail = (errorDescription ?? "").Trim();
        if (detail.Length > 180)
        {
            detail = detail[..180];
        }

        return string.IsNullOrWhiteSpace(detail)
            ? $"{Title(provider)} sign-in failed ({error})."
            : $"{Title(provider)} sign-in failed: {detail}";
    }

    private static string? GetSafeRedirectPath(string? value)
    {
        var redirect = value?.Trim();
        if (string.IsNullOrWhiteSpace(redirect) || !redirect.StartsWith('/') || redirect.StartsWith("//", StringComparison.Ordinal))
        {
            return null;
        }

        return redirect.Length > 500 ? redirect[..500] : redirect;
    }

    private static string? SanitizeAppReturn(string? value)
    {
        var redirect = value?.Trim();
        if (string.IsNullOrWhiteSpace(redirect))
        {
            return null;
        }

        if (redirect.StartsWith("dmbportfolio://", StringComparison.OrdinalIgnoreCase)
            || redirect.StartsWith("exp://", StringComparison.OrdinalIgnoreCase))
        {
            return redirect.Length > 500 ? redirect[..500] : redirect;
        }

        return null;
    }

    private static (string First, string Last) SplitName(string? given, string? family, string? full)
    {
        var first = (given ?? "").Trim();
        var last = (family ?? "").Trim();
        if (first.Length == 0 || last.Length == 0)
        {
            var parts = (full ?? "").Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (first.Length == 0)
            {
                first = parts.Length > 0 ? parts[0] : "Member";
            }

            if (last.Length == 0)
            {
                last = parts.Length > 1 ? string.Join(' ', parts.Skip(1)) : "Account";
            }
        }

        return (Clip(first, 100), Clip(last, 100));
    }

    private static string Clip(string value, int max)
    {
        var trimmed = value.Trim();
        return trimmed.Length <= max ? trimmed : trimmed[..max];
    }

    private static string? ClipPhone(string? phone)
    {
        var value = phone?.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        return value.Length <= 30 ? value : value[..30];
    }

    private static string CreateTicket() => ToBase64Url(RandomNumberGenerator.GetBytes(32));

    private static string HashToken(string value)
    {
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    }

    private static string ToBase64Url(byte[] data)
    {
        return Convert.ToBase64String(data).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private static byte[] FromBase64Url(string value)
    {
        var padded = value.Replace('-', '+').Replace('_', '/');
        switch (padded.Length % 4)
        {
            case 2: padded += "=="; break;
            case 3: padded += "="; break;
        }

        return Convert.FromBase64String(padded);
    }

    private static ExternalAuthVerifyResult Fail(string message, int status) => new()
    {
        Success = false,
        ErrorMessage = message,
        StatusCode = status
    };

    private sealed class OAuthProfile
    {
        public string ProviderUserId { get; init; } = "";
        public string? Email { get; init; }
        public string FirstName { get; init; } = "Member";
        public string LastName { get; init; } = "Account";
        public string? Phone { get; init; }
    }

    private sealed class OAuthStatePayload
    {
        [JsonPropertyName("n")]
        public string Nonce { get; set; } = "";

        [JsonPropertyName("c")]
        public string Client { get; set; } = "web";

        [JsonPropertyName("r")]
        public string? ReturnPath { get; set; }

        [JsonPropertyName("exp")]
        public long Exp { get; set; }
    }

    private sealed class OAuthTokenResponse
    {
        [JsonPropertyName("access_token")]
        public string? AccessToken { get; set; }
    }

    private sealed class GoogleUserInfo
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("email_verified")]
        public bool? EmailVerified { get; set; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; set; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class LinkedInUserInfo
    {
        [JsonPropertyName("sub")]
        public string? Sub { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("given_name")]
        public string? GivenName { get; set; }

        [JsonPropertyName("family_name")]
        public string? FamilyName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private sealed class FacebookUserInfo
    {
        [JsonPropertyName("id")]
        public string? Id { get; set; }

        [JsonPropertyName("email")]
        public string? Email { get; set; }

        [JsonPropertyName("first_name")]
        public string? FirstName { get; set; }

        [JsonPropertyName("last_name")]
        public string? LastName { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }
}
