using Microsoft.EntityFrameworkCore;
using Tennis.Domain.Entities;

namespace Tennis.Application.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserAuthIdentity> UserAuthIdentities => Set<UserAuthIdentity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(builder =>
        {
            builder.ToTable("users");
            builder.HasKey(user => user.Id);
            builder.Property(user => user.Email).HasMaxLength(320).IsRequired();
            builder.Property(user => user.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(user => user.CreatedUtc).IsRequired();
            builder.Property(user => user.LastSeenUtc).IsRequired();
            builder.HasIndex(user => user.Email).IsUnique();
            builder.HasMany(user => user.AuthIdentities)
                .WithOne(identity => identity.User)
                .HasForeignKey(identity => identity.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<UserAuthIdentity>(builder =>
        {
            builder.ToTable("user_auth_identities");
            builder.HasKey(identity => identity.Id);
            builder.Property(identity => identity.Provider).HasMaxLength(100).IsRequired();
            builder.Property(identity => identity.Subject).HasMaxLength(200).IsRequired();
            builder.Property(identity => identity.LinkedUtc).IsRequired();
            builder.HasIndex(identity => identity.Subject).IsUnique();
        });
    }
}
