namespace FileStorage.Infrastructure.Redis;

using FileStorage.Application.Cache;
using Microsoft.Extensions.Logging;

/// <summary>
/// Central, single-purpose cache invalidator. Called from BOTH the soft-delete
/// request path and the nightly purge job (and any future update path), so the
/// invalidation logic lives in exactly one place and is idempotent.
/// </summary>
public sealed class FileCacheInvalidator : IFileCacheInvalidator
{
    private readonly ICacheService _cache;
    private readonly ILogger<FileCacheInvalidator> _logger;

    public FileCacheInvalidator(ICacheService cache, ILogger<FileCacheInvalidator> logger)
    {
        _cache = cache;
        _logger = logger;
    }

    public async Task InvalidateFileCacheAsync(Guid fileId, string? shortCode = null, string? friendlyName = null, CancellationToken cancellationToken = default)
    {
        var keys = new List<string>();

        // Always remove the by-id key.
        keys.Add(CacheKeys.FileMetaById(fileId));

        // Remove by-code and by-name keys when known by the caller (avoids an index read).
        if (!string.IsNullOrWhiteSpace(shortCode))
        {
            keys.Add(CacheKeys.FileMetaByCode(shortCode));
        }

        if (!string.IsNullOrWhiteSpace(friendlyName))
        {
            keys.Add(CacheKeys.FileMetaByName(friendlyName));
        }

        // Belt-and-braces: read the per-file Redis index for any key we might not know about.
        var indexKey = CacheKeys.FileKeysIndex(fileId);
        var indexedKeys = await _cache.GetSetMembersAsync(indexKey, cancellationToken);
        keys.AddRange(indexedKeys);

        // Delete the index itself last so a re-run is a clean no-op (idempotent).
        keys.Add(indexKey);

        await _cache.RemoveAsync(keys.Distinct().ToArray());

        _logger.LogDebug("Invalidated {Count} cache key(s) for file {FileId}.", keys.Count, fileId);
    }
}