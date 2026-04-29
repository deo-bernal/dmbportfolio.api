using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dmb.Data.Entities
{
    [Table("UserDetails")]
    public class UserDetails
    {
        [Key]
        public int UserDetailsId { get; set; }

        [Required]
        public int UserId { get; set; }

        public string? Description { get; set; }

        public string? Skills { get; set; }

        public string? Video { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation property
        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
