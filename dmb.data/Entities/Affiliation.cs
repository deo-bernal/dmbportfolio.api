using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dmb.Data.Entities
{
    [Table("Affiliation")]
    public class Affiliation
    {
        [Key]
        public int AffiliationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Organization { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Title { get; set; } = null!;

        public DateTime? IssueDate { get; set; }

        [Required]
        public string Details { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}

