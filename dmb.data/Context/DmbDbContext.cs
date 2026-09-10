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
        public DbSet<WorkHistory> WorkHistories { get; set; } = null!;
        public DbSet<Education> Educations { get; set; } = null!;
        public DbSet<Affiliation> Affiliations { get; set; } = null!;
        public DbSet<ProjectType> ProjectTypes { get; set; } = null!;
        public DbSet<RevokedToken> RevokedTokens { get; set; } = null!;
        public DbSet<PasswordResetToken> PasswordResetTokens { get; set; } = null!;
        public DbSet<AccountActivationToken> AccountActivationTokens { get; set; } = null!;
        public DbSet<AppRefreshToken> AppRefreshTokens { get; set; } = null!;
        public DbSet<ExternalLogin> ExternalLogins { get; set; } = null!;
        public DbSet<PendingExternalLogin> PendingExternalLogins { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User indexes
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(u => u.Username).IsUnique();
                entity.HasIndex(u => u.Email).IsUnique();
                entity.HasIndex(u => new { u.Username, u.FirstName, u.LastName })
                    .IsUnique()
                    .HasDatabaseName("UX_User_Username_FirstName_LastName");
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

            modelBuilder.Entity<WorkHistory>(entity =>
            {
                entity.HasOne(w => w.User)
                    .WithMany(u => u.WorkHistories)
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Education>(entity =>
            {
                entity.HasOne(e => e.User)
                    .WithMany(u => u.Educations)
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Affiliation>(entity =>
            {
                entity.HasOne(a => a.User)
                    .WithMany(u => u.Affiliations)
                    .HasForeignKey(a => a.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<RevokedToken>(entity =>
            {
                entity.HasIndex(token => token.Jti).IsUnique();
                entity.HasOne(token => token.User)
                    .WithMany()
                    .HasForeignKey(token => token.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
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

            modelBuilder.Entity<AppRefreshToken>(entity =>
            {
                entity.HasIndex(t => t.RefreshTokenHash).IsUnique();
                entity.HasOne(t => t.User)
                    .WithMany(u => u.AppRefreshTokens)
                    .HasForeignKey(t => t.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(t => t.CreatedAt).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<ExternalLogin>(entity =>
            {
                entity.HasIndex(l => new { l.Provider, l.ProviderUserId })
                    .IsUnique()
                    .HasDatabaseName("UX_ExternalLogin_Provider_ProviderUserId");
                entity.HasIndex(l => new { l.UserId, l.Provider })
                    .IsUnique()
                    .HasDatabaseName("UX_ExternalLogin_UserId_Provider");
                entity.HasOne(l => l.User)
                    .WithMany(u => u.ExternalLogins)
                    .HasForeignKey(l => l.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.Property(l => l.CreatedAt).ValueGeneratedOnAdd();
            });

            modelBuilder.Entity<PendingExternalLogin>(entity =>
            {
                entity.HasIndex(p => p.Ticket).IsUnique().HasDatabaseName("UX_PendingExternalLogin_Ticket");
                entity.Property(p => p.CreatedAt).ValueGeneratedOnAdd();
            });

            // Configure CreatedAt to be generated on add (consumer may set default in DB/provider)
            modelBuilder.Entity<User>().Property(u => u.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<UserDetails>().Property(ud => ud.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<Project>().Property(p => p.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<WorkHistory>().Property(w => w.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<Education>().Property(e => e.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<Affiliation>().Property(a => a.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<ProjectType>().Property(pt => pt.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<RevokedToken>().Property(token => token.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<PasswordResetToken>().Property(t => t.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<AccountActivationToken>().Property(t => t.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<AppRefreshToken>().Property(t => t.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<ExternalLogin>().Property(l => l.CreatedAt).ValueGeneratedOnAdd();
            modelBuilder.Entity<PendingExternalLogin>().Property(p => p.CreatedAt).ValueGeneratedOnAdd();
        }
    }
}
