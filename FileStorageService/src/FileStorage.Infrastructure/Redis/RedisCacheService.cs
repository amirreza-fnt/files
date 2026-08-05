namespace FileStorage.Infrastructure.Redis;

using FileStorage.Application.Cache;
using FileStorage.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

/// <summary>
/// Redis implementation of <see cref="ICacheService"/> for the read-through DB
/// cache (database index = RedisOptions.DbCacheDatabase). Temporary/upload
/// tokens live in a separate database index (RedisOptions.TokenDatabase).
///
/// Degrades gracefully: when Redis is briefly unavailable, reads fall through
/// (treated as a cache miss → MSSQL), and writes are skipped. The application
/// never crashes because Redis is down.
/// </summary>
public sealed class RedisCacheService : ICacheService
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly int _database;
    private readonly ILogger<RedisCacheService> _logger;

    public RedisCacheService(IConnectionMultiplexer multiplexer, IOptions<RedisOptions> options, ILogger<RedisCacheService> logger)
    {
        _multiplexer = multiplexer;
        _database = options.Value.DbCacheDatabase;
        _logger = logger;
    }

    private IDatabase Db => _multiplexer.GetDatabase(_database);

    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        var value = await TryGetAsync(key, cancellationToken);
        if (value is null)
        {
            return default;
        }

        return RedisJson.Deserialize<T>(value);
    }

    public async Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
        => await TryGetAsync(key, cancellationToken);

    public async Task<bool> GetBoolAsync(string key, CancellationToken cancellationToken = default)
    {
        var value = await TryGetAsync(key, cancellationToken);
        return value is "1" or "true" or "True";
    }

    public async Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
        => await TryWriteAsync(async () => { await Db.StringSetAsync(key, RedisJson.Serialize(value), ttl); });

    public async Task SetStringAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default)
        => await TryWriteAsync(async () => { await Db.StringSetAsync(key, value, ttl); });

    public async Task RemoveAsync(params string[] keys)
    {
        if (keys.Length == 0)
        {
            return;
        }

        var redisKeys = keys.Where(k => !string.IsNullOrWhiteSpace(k)).Select(k => (RedisKey)k).ToArray();
        if (redisKeys.Length > 0)
        {
            await TryWriteAsync(async () => { await Db.KeyDeleteAsync(redisKeys); });
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            return await Db.KeyExistsAsync(key);
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable during Exists — treating as miss.");
            return false;
        }
    }

    public async Task AddToIndexAsync(string indexKey, string member, TimeSpan ttl, CancellationToken cancellationToken = default)
        => await TryWriteAsync(async () =>
        {
            await Db.SetAddAsync(indexKey, member);
            await Db.KeyExpireAsync(indexKey, ttl);
        });

    public async Task<IEnumerable<string>> GetSetMembersAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            var members = await Db.SetMembersAsync(key);
            return members.Select(m => m.ToString());
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable during GetSetMembers — returning empty index.");
            return Enumerable.Empty<string>();
        }
    }

    private async Task<string?> TryGetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            var value = await Db.StringGetAsync(key);
            return value.IsNullOrEmpty ? null : value.ToString();
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable during read of {Key} — falling through to MSSQL.", key);
            return null;
        }
    }

    private async Task TryWriteAsync(Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (RedisException ex)
        {
            _logger.LogWarning(ex, "Redis unavailable during write — skipping cache write.");
        }
    }
}