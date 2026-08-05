namespace FileStorage.Application.Services;

using FileStorage.Application.Cache;
using FileStorage.Application.Interfaces;
using FileStorage.Application.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public interface IFileDeleteService
{
    /// <summary>
    /// Soft delete: flips IsDeleted + DeletedAtUtc, then immediately invalidates
    /// every Redis key for the file so no request can see a stale "exists" row.
    /// </summary>
    Task<bool> SoftDeleteAsync(Guid fileId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Purges soft-deleted rows older than the grace period. Deletes physical
    /// bytes first, then the DB row, and re-invalidates cache (idempotent).
    /// Called by the nightly Hangfire recurring job.
    /// </summary>
    Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default);
}

public sealed class FileDeleteService : IFileDeleteService
{
    private readonly IFileRepository _fileRepository;
    private readonly IFileAuthorizationService _authorization;
    private readonly IFileStorage _fileStorage;
    private readonly IFileCacheInvalidator _cacheInvalidator;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly DeleteOptions _deleteOptions;
    private readonly ILogger<FileDeleteService> _logger;

    public FileDeleteService(
        IFileRepository fileRepository,
        IFileAuthorizationService authorization,
        IFileStorage fileStorage,
        IFileCacheInvalidator cacheInvalidator,
        ICurrentUserAccessor currentUser,
        IOptions<DeleteOptions> deleteOptions,
        ILogger<FileDeleteService> logger)
    {
        _fileRepository = fileRepository;
        _authorization = authorization;
        _fileStorage = fileStorage;
        _cacheInvalidator = cacheInvalidator;
        _currentUser = currentUser;
        _deleteOptions = deleteOptions.Value;
        _logger = logger;
    }

    public async Task<bool> SoftDeleteAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        // Load (including soft-deleted so re-delete / idempotency is possible).
        var file = await _fileRepository.GetByIdIncludingDeletedAsync(fileId, cancellationToken);
        if (file is null)
        {
            return false;
        }

        if (!_authorization.CanManage(file.OwnerUserId, _currentUser.UserId, _currentUser.IsAdmin))
        {
            return false;
        }

        file.SoftDelete(DateTime.UtcNow);
        await _fileRepository.UpdateAsync(file, cancellationToken);

        // IMMEDIATE invalidation: no later request may see the cached "alive" version.
        await _cacheInvalidator.InvalidateFileCacheAsync(file.Id, file.ShortCode, file.FriendlyName, cancellationToken);

        return true;
    }

    public async Task<int> PurgeExpiredAsync(CancellationToken cancellationToken = default)
    {
        var threshold = DateTime.UtcNow.AddHours(-_deleteOptions.GracePeriodHours);
        var purged = 0;

        while (true)
        {
            var candidates = await _fileRepository.GetDeletedOlderThanAsync(threshold, take: 100, cancellationToken);
            if (candidates.Count == 0)
            {
                break;
            }

            foreach (var file in candidates)
            {
                try
                {
                    // Physical bytes first — best effort; missing file is fine, data is expendable.
                    await _fileStorage.DeleteAsync(file.StoragePath, cancellationToken);
                }
                catch (Exception ex)
                {
                    // Only log: if the blob is already gone, we still remove the row.
                    _logger.LogWarning(ex, "Physical delete failed for file {FileId}; removing DB row anyway.", file.Id);
                }

                await _fileRepository.HardDeleteAsync(file.Id, cancellationToken);

                // Re-invalidate (idempotent) so no trace remains in Redis even if the
                // request-time invalidation failed earlier for any reason.
                await _cacheInvalidator.InvalidateFileCacheAsync(file.Id, file.ShortCode, file.FriendlyName, cancellationToken);

                purged++;
            }
        }

        return purged;
    }
}