using Microsoft.EntityFrameworkCore;
using Tennis.Domain.Entities;

namespace Tennis.Application.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<UserAuthIdentity> UserAuthIdentities => Set<UserAuthIdentity>();
    public DbSet<Ladder> Ladders => Set<Ladder>();
    public DbSet<LadderMembership> LadderMemberships => Set<LadderMembership>();

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

        modelBuilder.Entity<Ladder>(builder =>
        {
            builder.ToTable("ladders", tableBuilder =>
                tableBuilder.HasCheckConstraint(
                    "CK_ladders_status",
                    "\"Status\" IN ('Draft', 'Active')"));
            builder.HasKey(ladder => ladder.Id);
            builder.Property(ladder => ladder.Name).HasMaxLength(120).IsRequired();
            builder.Property(ladder => ladder.Status)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            builder.Property(ladder => ladder.CreatedUtc).IsRequired();
            builder.HasOne(ladder => ladder.CreatedByUser)
                .WithMany(user => user.CreatedLadders)
                .HasForeignKey(ladder => ladder.CreatedByUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<LadderMembership>(builder =>
        {
            builder.ToTable("ladder_memberships", tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_ladder_memberships_role",
                    "\"Role\" IN ('Organizer', 'Player')");
                tableBuilder.HasCheckConstraint(
                    "CK_ladder_memberships_position",
                    "(\"Role\" = 'Organizer' AND \"Position\" IS NULL) OR " +
                    "(\"Role\" = 'Player' AND \"Position\" > 0)");
            });
            builder.HasKey(membership => membership.Id);
            builder.Property(membership => membership.Role)
                .HasConversion<string>()
                .HasMaxLength(20)
                .IsRequired();
            builder.Property(membership => membership.DisplayName).HasMaxLength(200).IsRequired();
            builder.Property(membership => membership.CreatedUtc).IsRequired();
            builder.HasIndex(membership => new
                {
                    membership.LadderId,
                    membership.UserId,
                    membership.Role
                })
                .IsUnique();
            builder.HasIndex(membership => new
                {
                    membership.LadderId,
                    membership.Position
                })
                .IsUnique()
                .HasFilter("\"Position\" IS NOT NULL");
            builder.HasOne(membership => membership.Ladder)
                .WithMany(ladder => ladder.Memberships)
                .HasForeignKey(membership => membership.LadderId)
                .OnDelete(DeleteBehavior.Cascade);
            builder.HasOne(membership => membership.User)
                .WithMany(user => user.LadderMemberships)
                .HasForeignKey(membership => membership.UserId)
                .OnDelete(DeleteBehavior.Restrict);
        });
    }
}
