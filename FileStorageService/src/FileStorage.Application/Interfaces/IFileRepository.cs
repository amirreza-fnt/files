namespace FileStorage.Application.Interfaces;

using FileStorage.Application.Dtos;
using FileStorage.Domain.Entities;

/// <summary>
/// File repository. Infrastructure implements cache-aside (read Redis first),
/// so callers never touch MSSQL directly for hot reads.
/// </summary>
public interface IFileRepository
{
    Task<FileItem?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);

    Task<FileItem?> GetByFriendlyNameAsync(string friendlyName, CancellationToken cancellationToken = default);

    Task<FileItem?> GetByIdAsync(Guid fileId, CancellationToken cancellationToken = default);

    /// <summary>Fetches the row for ownership/authorization checks, no cache required.</summary>
    Task<FileItem?> GetByIdIncludingDeletedAsync(Guid fileId, CancellationToken cancellationToken = default);

    Task<bool> IsShortCodeFreeAsync(string shortCode, CancellationToken cancellationToken = default);

    Task<bool> IsFriendlyNameFreeAsync(string friendlyName, CancellationToken cancellationToken = default);

    Task<FileItem> CreateAsync(FileItem file, CancellationToken cancellationToken = default);

    Task UpdateAsync(FileItem file, CancellationToken cancellationToken = default);

    /// <summary>Gets soft-deleted rows older than the grace period (used by nightly purge job).</summary>
    Task<IReadOnlyList<FileItem>> GetDeletedOlderThanAsync(DateTime thresholdUtc, int take, CancellationToken cancellationToken = default);

    /// <summary>Hard-removes a row. Idempotent.</summary>
    Task HardDeleteAsync(Guid fileId, CancellationToken cancellationToken = default);
}