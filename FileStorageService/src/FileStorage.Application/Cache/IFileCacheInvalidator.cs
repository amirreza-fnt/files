namespace FileStorage.Application.Cache;

/// <summary>
/// Central cache invalidation seam. Every path that changes or deletes a file
/// MUST call <see cref="InvalidateFileCacheAsync"/> so Redis never serves a
/// stale version of data that no longer exists in MSSQL.
/// </summary>
public interface IFileCacheInvalidator
{
    /// <summary>
    /// Deletes every Redis key that could represent the given file.
    /// Idempotent: missing keys are simply ignored.
    /// </summary>
    /// <param name="fileId">File identifier.</param>
    /// <param name="shortCode">Optional known short code (skips index lookup).</param>
    /// <param name="friendlyName">Optional known friendly name (skips index lookup).</param>
    Task InvalidateFileCacheAsync(Guid fileId, string? shortCode = null, string? friendlyName = null, CancellationToken cancellationToken = default);
}