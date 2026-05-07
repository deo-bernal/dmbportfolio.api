using Microsoft.EntityFrameworkCore;
using Dmb.Data.Entities;

namespace Dmb.Data.Context
{
    public class DmbDbContext : DbContext
    {
        public DmbDbContext(DbContextOptions<DmbDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } = null!;
        public DbSet<UserDetails> UserDetails { get; set; } = null!;
        public DbSet<Project> Projects { get; set; } = null!;
        public DbSet<ProjectType> ProjectTypes { get; set; } = null!;
        public DbSet<RevokedToken> RevokedTokens { get; set; } = null!;
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
        public DbSet<AccountActivationToken> AccountActivationTokens { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User indexes
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
            });

            // UserDetails: one-to-one with User, unique UserId
            modelBuilder.Entity<UserDetails>(entity =>
            {
                entity.HasIndex(ud => ud.UserId).IsUnique();
                entity.HasOne(ud => ud.User)
                      .WithOne(u => u.UserDetails)
                      .HasForeignKey<UserDetails>(ud => ud.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ProjectType>(entity =>
            {
                entity.HasIndex(pt => pt.TypeName).IsUnique();
            });

            // Project: many-to-one with User, many-to-one with ProjectType
            modelBuilder.Entity<Project>(entity =>
            {
                entity.HasIndex(p => new { p.UserId, p.Name, p.ProjectTypeId }).IsUnique().HasDatabaseName("UQ_Project_User_Name_ProjectType");
                entity.HasOne(p => p.User)
                      .WithMany(u => u.Projects)
                      .HasForeignKey(p => p.UserId)
                      .OnDelete(DeleteBehavior.Cascade);
                entity.HasOne(p => p.ProjectType)
                      .WithMany(pt => pt.Projects)
                      .HasForeignKey(p => p.ProjectTypeId)
                      .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RevokedToken>(entity =>
            {
                entity.HasIndex(token => token.Jti).IsUnique();
            });

            modelBuilder.Entity<PasswordResetToken>(entity =>
            {
                entity.HasIndex(t => t.Token).IsUnique().HasDatabaseName("UX_PasswordResetToken_Token");
                entity.HasOne(t => t.User)
                    .WithMany(u => u.PasswordResetTokens)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(t => t.CreatedAt).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<AccountActivationToken>(entity =>
            {
                entity.HasIndex(t => t.Token).IsUnique().HasDatabaseName("UX_AccountActivationToken_Token");
                entity.HasOne(t => t.User)
                    .WithMany(u => u.AccountActivationTokens)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(t => t.CreatedAt).ValueGeneratedOnAdd();
            });

            // Configure CreatedAt to be generated on add (consumer may set default in DB/provider)
            modelBuilder.Entity<User>().Property(u => u.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<UserDetails>().Property(ud => ud.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<Project>().Property(p => p.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<ProjectType>().Property(pt => pt.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<RevokedToken>().Property(token => token.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<PasswordResetToken>().Property(t => t.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<AccountActivationToken>().Property(t => t.CreatedAt).ValueGeneratedOnAdd();
        }
    }
}
