using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Core.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options):base(options){}

    public DbSet<UserSession> UserSessions => Set<UserSession>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<UserSession>(entity =>
        {
            entity.HasKey(session => session.Id);

            entity.Property(session => session.UserId)
                .IsRequired();

            entity.Property(session => session.IpAddress)
                .HasMaxLength(64);

            entity.Property(session => session.UserAgent)
                .HasMaxLength(1024);

            entity.Property(session => session.Browser)
                .HasMaxLength(64);

            entity.Property(session => session.OperatingSystem)
                .HasMaxLength(64);

            entity.Property(session => session.Device)
                .HasMaxLength(64);

            entity.Property(session => session.RevokedByUserId)
                .HasMaxLength(450);

            entity.Property(session => session.RevokeReason)
                .HasMaxLength(256);

            entity.HasOne(session => session.User)
                .WithMany()
                .HasForeignKey(session => session.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasIndex(session => session.UserId);
            entity.HasIndex(session => session.RevokedAtUtc);
            entity.HasIndex(session => session.ExpiresAtUtc);
        });

        builder.Entity<IdentityUserLogin<string>>(entity =>
        {
            entity.Property(login => login.LoginProvider)
                .HasMaxLength(128);

            entity.Property(login => login.ProviderKey)
                .HasMaxLength(128);
        });

        builder.Entity<IdentityUserToken<string>>(entity =>
        {
            entity.Property(token => token.LoginProvider)
                .HasMaxLength(128);

            entity.Property(token => token.Name)
                .HasMaxLength(128);
        });
    }
}
