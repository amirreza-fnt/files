namespace FileStorage.Infrastructure.Persistence;

using FileStorage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

/// <summary>
/// EF Core DbContext. The single database is shared by all three services
/// (Upload / Read / Delete) — logical separation only, per architecture decision.
/// </summary>
public sealed class FileStorageDbContext : DbContext
{
    public FileStorageDbContext(DbContextOptions<FileStorageDbContext> options)
        : base(options)
    {
    }

    public DbSet<FileItem> Files => Set<FileItem>();
    public DbSet<StaticFile> StaticFiles => Set<StaticFile>();
    public DbSet<FileGroup> FileGroups => Set<FileGroup>();
    public DbSet<FileGroupMember> FileGroupMembers => Set<FileGroupMember>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Soft-delete global filter — deleted files are invisible everywhere.
        modelBuilder.Entity<FileItem>()
            .HasQueryFilter(f => !f.IsDeleted);

        modelBuilder.Entity<FileItem>(e =>
        {
            e.ToTable("Files");
            e.HasKey(x => x.Id);

            e.Property(x => x.FriendlyName).HasMaxLength(255).IsRequired();
            e.HasIndex(x => x.FriendlyName).IsUnique();

            e.Property(x => x.ShortCode).HasMaxLength(16).IsRequired();
            e.HasIndex(x => x.ShortCode).IsUnique();

            e.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
            e.Property(x => x.Extension).HasMaxLength(16).IsRequired();
            e.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
            e.Property(x => x.StoragePath).HasMaxLength(1024).IsRequired();
            e.Property(x => x.PhysicalName).HasMaxLength(255).IsRequired();

            e.Property(x => x.AccessType).HasConversion<string>().HasMaxLength(24);
            e.Property(x => x.OwnerUserId);
            e.HasIndex(x => x.OwnerUserId);
            e.Property(x => x.GroupId);
            e.HasIndex(x => x.GroupId);

            e.Property(x => x.IsDeleted).HasDefaultValue(false);
            e.HasIndex(x => new { x.IsDeleted, x.DeletedAtUtc });

            // rowversion auto-generation is SQL-Server specific.
            if (Database.IsSqlServer())
            {
                e.Property(x => x.RowVersion).IsRowVersion();
            }
            else
            {
                e.Property(x => x.RowVersion).IsConcurrencyToken();
            }

            e.HasOne<FileGroup>()
                .WithMany()
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        modelBuilder.Entity<StaticFile>(e =>
        {
            e.ToTable("StaticFiles");
            e.HasKey(x => x.Id);

            e.Property(x => x.ShortCode).HasMaxLength(16).IsRequired();
            e.HasIndex(x => x.ShortCode).IsUnique();
            e.Property(x => x.StoragePath).HasMaxLength(1024).IsRequired();
            e.Property(x => x.PhysicalName).HasMaxLength(255).IsRequired();
            e.Property(x => x.MimeType).HasMaxLength(100).IsRequired();
            e.Property(x => x.CacheControlSeconds).HasDefaultValue(86400);
            if (Database.IsSqlServer())
            {
                e.Property(x => x.RowVersion).IsRowVersion();
            }
            else
            {
                e.Property(x => x.RowVersion).IsConcurrencyToken();
            }
        });

        modelBuilder.Entity<FileGroup>(e =>
        {
            e.ToTable("FileGroups");
            e.HasKey(x => x.Id);
            e.Property(x => x.Name).HasMaxLength(120).IsRequired();
            e.Property(x => x.OwnerUserId);
        });

        modelBuilder.Entity<FileGroupMember>(e =>
        {
            e.ToTable("FileGroupMembers");
            e.HasKey(x => x.Id);
            e.Property(x => x.UserId);
            e.HasIndex(x => new { x.GroupId, x.UserId }).IsUnique();

            e.HasOne(x => x.Group)
                .WithMany(x => x.Members)
                .HasForeignKey(x => x.GroupId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}