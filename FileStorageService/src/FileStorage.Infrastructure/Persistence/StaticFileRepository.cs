namespace FileStorage.Infrastructure.Persistence;

using FileStorage.Application.Cache;
using FileStorage.Application.Interfaces;
using FileStorage.Application.Options;
using FileStorage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// Static-file repository with cache-aside. Static metadata changes rarely, so
/// it uses a much longer TTL than regular file metadata.
/// </summary>
public sealed class StaticFileRepository : IStaticFileRepository
{
    private readonly FileStorageDbContext _context;
    private readonly ICacheService _cache;
    private readonly CacheOptions _options;

    public StaticFileRepository(FileStorageDbContext context, ICacheService cache, IOptions<CacheOptions> options)
    {
        _context = context;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<StaticFile?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.StaticFileByCode(shortCode);
        var cached = await _cache.GetAsync<StaticFile>(key, cancellationToken);
        if (cached is not null)
        {
            return cached;
        }

        var file = await _context.StaticFiles.SingleOrDefaultAsync(f => f.ShortCode == shortCode, cancellationToken);
        if (file is not null)
        {
            await _cache.SetAsync(key, file, _options.StaticFileTtl, cancellationToken);
        }

        return file;
    }
}