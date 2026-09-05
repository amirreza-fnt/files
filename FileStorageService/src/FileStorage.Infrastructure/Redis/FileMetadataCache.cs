namespace FileStorage.Infrastructure.Redis;

using FileStorage.Application.Cache;
using FileStorage.Application.Dtos;
using FileStorage.Application.Interfaces;
using FileStorage.Application.Options;
using FileStorage.Domain.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Cache-Aside implementation for file metadata:
///   1. Try Redis (<c>file:meta:*</c>).
///   2. On miss, read MSSQL.
///   3. On hit-to-miss transition, warm Redis.
/// Every warmed key is recorded in a per-file index (<c>file:keys:*</c>) so the
/// central invalidator can remove all of them reliably later.
/// </summary>
public sealed class FileMetadataCache : IFileMetadataCache
{
    private readonly ICacheService _cache;
    private readonly IFileRepository _repository;
    private readonly CacheOptions _options;
    private readonly ILogger<FileMetadataCache> _logger;

    public FileMetadataCache(ICacheService cache, IFileRepository repository, IOptions<CacheOptions> options, ILogger<FileMetadataCache> logger)
    {
        _cache = cache;
        _repository = repository;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<CachedFileMeta?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.FileMetaByCode(shortCode);
        var cached = await _cache.GetAsync<CachedFileMeta>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var file = await _repository.GetByShortCodeAsync(shortCode, cancellationToken);
        if (file is null)
        {
            return null;
        }

        await WarmAsync(file, cancellationToken);
        return ToMeta(file);
    }

    public async Task<CachedFileMeta?> GetByFriendlyNameAsync(string friendlyName, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.FileMetaByName(friendlyName);
        var cached = await _cache.GetAsync<CachedFileMeta>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var file = await _repository.GetByFriendlyNameAsync(friendlyName, cancellationToken);
        if (file is null)
        {
            return null;
        }

        await WarmAsync(file, cancellationToken);
        return ToMeta(file);
    }

    public async Task<CachedFileMeta?> GetByIdAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.FileMetaById(fileId);
        var cached = await _cache.GetAsync<CachedFileMeta>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var file = await _repository.GetByIdAsync(fileId, cancellationToken);
        if (file is null)
        {
            return null;
        }

        await WarmAsync(file, cancellationToken);
        return ToMeta(file);
    }

    public async Task WarmAsync(FileItem file, CancellationToken cancellationToken = default)
    {
        var meta = ToMeta(file);
        var ttl = _options.FileMetadataTtl;
        var indexTtl = _options.FileKeysIndexTtl;

        var keys = new[]
        {
            CacheKeys.FileMetaByCode(file.ShortCode),
            CacheKeys.FileMetaByName(file.FriendlyName),
            CacheKeys.FileMetaById(file.Id),
        };

        foreach (var key in keys)
        {
            await _cache.SetAsync(key, meta, ttl, cancellationToken);

            // Record the key in the invalidation index (only if the file is alive).
            if (!file.IsDeleted)
            {
                await _cache.AddToIndexAsync(CacheKeys.FileKeysIndex(file.Id), key, indexTtl, cancellationToken);
            }
        }
    }

    private static CachedFileMeta ToMeta(FileItem file)
        => new(
            file.Id,
            file.ShortCode,
            file.FriendlyName,
            file.Title,
            file.Description,
            file.OriginalFileName,
            file.Extension,
            file.MimeType,
            file.SizeBytes,
            file.StoragePath,
            file.AccessType,
            file.OwnerUserId,
            file.GroupId,
            file.IsDeleted,
            file.CreatedAtUtc,
            file.VersionToken);
}