using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dmb.Data.Entities;

[Table("AccountActivationToken")]
public class AccountActivationToken
{
    [Key]
    public int TokenId { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(256)]
    public string Token { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }

    public bool IsUsed { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public User User { get; set; } = null!;
}
