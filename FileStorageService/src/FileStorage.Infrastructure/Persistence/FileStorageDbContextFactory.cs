namespace FileStorage.Infrastructure.Persistence;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

/// <summary>
/// Design-time factory used by <c>dotnet ef</c> on the AlmaLinux host and in CI.
/// Connection string is read from the FILE_STORAGE_CONNECTION environment variable
/// (set by systemd / docker-compose), falling back to appsettings for local dev.
/// </summary>
public sealed class FileStorageDbContextFactory : IDesignTimeDbContextFactory<FileStorageDbContext>
{
    public FileStorageDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("FILE_STORAGE_CONNECTION")
            ?? "Server=localhost;Database=apiwebfilestorage;User Id=sa;Password=ChangeMe123!;Encrypt=True;TrustServerCertificate=True;";

        var options = new DbContextOptionsBuilder<FileStorageDbContext>()
            .UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure(5, TimeSpan.FromSeconds(10), null))
            .Options;

        return new FileStorageDbContext(options);
    }
}