using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dmb.Data.Entities
{
    [Table("ProjectType")]
    public class ProjectType
    {
        [Key]
        public int ProjectTypeId { get; set; }

        [Required]
        [MaxLength(100)]
        public string TypeName { get; set; } = null!;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public ICollection<Project> Projects { get; set; } = new List<Project>();
    }
}
