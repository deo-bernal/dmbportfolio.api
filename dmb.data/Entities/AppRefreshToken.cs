using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dmb.Data.Entities;

[Table("AppRefreshToken")]
public class AppRefreshToken
{
    [Key]
    public int AppRefreshTokenId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(128)]
    public string RefreshTokenHash { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }
    public DateTimeOffset? RevokedAt { get; set; }
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
