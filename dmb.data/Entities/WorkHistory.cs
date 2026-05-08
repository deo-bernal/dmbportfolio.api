using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dmb.Data.Entities
{
    [Table("WorkHistory")]
    public class WorkHistory
    {
        [Key]
        public int WorkHistoryId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string Company { get; set; } = null!;

        [Required]
        [MaxLength(200)]
        public string Position { get; set; } = null!;

        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? JobDescription { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
