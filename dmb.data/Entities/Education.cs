using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dmb.Data.Entities
{
    [Table("Education")]
    public class Education
    {
        [Key]
        public int EducationId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        [MaxLength(200)]
        public string School { get; set; } = null!;

        [MaxLength(255)]
        public string? Address { get; set; }

        [MaxLength(255)]
        public string? CourseTaken { get; set; }

        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        [ForeignKey(nameof(UserId))]
        public User? User { get; set; }
    }
}
