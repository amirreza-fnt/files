namespace FileStorage.Application.Interfaces;

using FileStorage.Application.Dtos;
using FileStorage.Domain.Entities;

/// <summary>
/// Cache-Aside read-through cache for file metadata. Reads first check Redis
/// (<c>file:meta:*</c>), and only on a miss fall back to MSSQL. Writes warm the
/// cache immediately (Upload path).
/// </summary>
public interface IFileMetadataCache
{
    Task<CachedFileMeta?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);

    Task<CachedFileMeta?> GetByFriendlyNameAsync(string friendlyName, CancellationToken cancellationToken = default);

    Task<CachedFileMeta?> GetByIdAsync(Guid fileId, CancellationToken cancellationToken = default);

    /// <summary>Populates Redis from a freshly-created/updated file row.</summary>
    Task WarmAsync(FileItem file, CancellationToken cancellationToken = default);
}