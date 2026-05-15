using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SampleClient.Models;

namespace SampleClient.Data;

public sealed class SampleClientDbContext : IdentityDbContext<SampleApplicationUser, IdentityRole, string>
{
    public SampleClientDbContext(DbContextOptions<SampleClientDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<SampleApplicationUser>(entity =>
        {
            entity.HasIndex(user => user.SsoSubject)
                .IsUnique()
                .HasFilter("\"SsoSubject\" IS NOT NULL");

            entity.Property(user => user.DisplayName)
                .HasMaxLength(200);

            entity.Property(user => user.SsoSubject)
                .HasMaxLength(200);
        });
    }
}
