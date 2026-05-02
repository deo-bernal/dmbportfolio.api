using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace Dmb.Data.Entities
{
    [Table("User")]
    [Index(nameof(Username), IsUnique = true)]
    [Index(nameof(Email), IsUnique = true)]
    public class User
    {
        [Key]
        public int UserId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Username { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string PasswordHash { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string PasswordSalt { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string FirstName { get; set; } = null!;

        [Required]
        [MaxLength(100)]
        public string LastName { get; set; } = null!;

        [Required]
        [MaxLength(255)]
        public string Email { get; set; } = null!;

        [MaxLength(30)]
        public string? ContactNo { get; set; }

        public bool Activated { get; set; }

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Navigation properties
        public UserDetails? UserDetails { get; set; }
        public ICollection<Project> Projects { get; set; } = new List<Project>();
        public ICollection<PasswordResetToken> PasswordResetTokens { get; set; } = new List<PasswordResetToken>();
    }
}
