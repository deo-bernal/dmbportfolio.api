namespace Dmb.Model;

/// <summary>
/// Domain shape for a password reset token. The EF-mapped entity lives in Dmb.Data.Entities
/// with the same columns and a navigation to User.
/// </summary>
public class PasswordResetToken
{
    public int TokenId { get; set; }
    public int UserId { get; set; }
    public string Token { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public bool IsUsed { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
}
