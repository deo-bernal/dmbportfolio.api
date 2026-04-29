using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Dmb.Data.Entities;

[Table("RevokedToken")]
[Index(nameof(Jti), IsUnique = true)]
public class RevokedToken
{
    [Key]
    public int RevokedTokenId { get; set; }

    [Required]
    public string Jti { get; set; } = null!;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
