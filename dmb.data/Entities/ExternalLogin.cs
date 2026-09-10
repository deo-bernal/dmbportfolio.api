using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Dmb.Data.Entities;

[Table("ExternalLogin")]
[Index(nameof(Provider), nameof(ProviderUserId), IsUnique = true, Name = "UX_ExternalLogin_Provider_ProviderUserId")]
[Index(nameof(UserId), nameof(Provider), IsUnique = true, Name = "UX_ExternalLogin_UserId_Provider")]
public class ExternalLogin
{
    [Key]
    public int ExternalLoginId { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    [MaxLength(32)]
    public string Provider { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string ProviderUserId { get; set; } = null!;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    [ForeignKey(nameof(UserId))]
    public User User { get; set; } = null!;
}
