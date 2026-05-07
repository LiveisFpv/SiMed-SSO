using Core.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Core.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options):base(options){}

    public DbSet<UserSession> UserSessions => Set<UserSession>();
    public DbSet<OAuthClient> OAuthClients => Set<OAuthClient>();
    public DbSet<OAuthClientRedirectUri> OAuthClientRedirectUris => Set<OAuthClientRedirectUri>();
    public DbSet<OAuthClientScope> OAuthClientScopes => Set<OAuthClientScope>();

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

        builder.Entity<OAuthClient>(entity =>
        {
            entity.HasKey(client => client.Id);
            entity.HasAlternateKey(client => client.ClientId);

            entity.Property(client => client.ClientId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(client => client.ClientSecretHash)
                .IsRequired();

            entity.Property(client => client.DisplayName)
                .IsRequired()
                .HasMaxLength(200);

            entity.Property(client => client.Description)
                .HasMaxLength(1000);

            entity.Property(client => client.CreatedByUserId)
                .HasMaxLength(450);

        });

        builder.Entity<OAuthClientRedirectUri>(entity =>
        {
            entity.HasKey(redirectUri => new { redirectUri.ClientId, redirectUri.Uri });

            entity.Property(redirectUri => redirectUri.ClientId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(redirectUri => redirectUri.Uri)
                .IsRequired()
                .HasMaxLength(2048);

            entity.HasOne(redirectUri => redirectUri.Client)
                .WithMany(client => client.RedirectUris)
                .HasForeignKey(redirectUri => redirectUri.ClientId)
                .HasPrincipalKey(client => client.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<OAuthClientScope>(entity =>
        {
            entity.HasKey(scope => new { scope.ClientId, scope.Scope });

            entity.Property(scope => scope.ClientId)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(scope => scope.Scope)
                .IsRequired()
                .HasMaxLength(100);

            entity.HasOne(scope => scope.Client)
                .WithMany(client => client.Scopes)
                .HasForeignKey(scope => scope.ClientId)
                .HasPrincipalKey(client => client.ClientId)
                .OnDelete(DeleteBehavior.Cascade);
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
