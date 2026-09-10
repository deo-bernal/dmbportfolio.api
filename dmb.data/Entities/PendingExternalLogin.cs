using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Dmb.Data.Entities;

[Table("PendingExternalLogin")]
[Index(nameof(Ticket), IsUnique = true, Name = "UX_PendingExternalLogin_Ticket")]
public class PendingExternalLogin
{
    [Key]
    public int PendingExternalLoginId { get; set; }

    [Required]
    [MaxLength(128)]
    public string Ticket { get; set; } = null!;

    [Required]
    [MaxLength(32)]
    public string Provider { get; set; } = null!;

    [Required]
    [MaxLength(128)]
    public string ProviderUserId { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string FirstName { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string LastName { get; set; } = null!;

    [MaxLength(30)]
    public string? Phone { get; set; }

    [Required]
    [MaxLength(16)]
    public string Client { get; set; } = null!;

    [MaxLength(500)]
    public string? ReturnPath { get; set; }

    [MaxLength(255)]
    public string? Email { get; set; }

    [MaxLength(128)]
    public string? CodeHash { get; set; }

    public DateTimeOffset? CodeExpiresAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
