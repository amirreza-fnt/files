namespace FileStorage.Infrastructure.Redis;

using System.Security.Cryptography;
using FileStorage.Application.Cache;
using FileStorage.Application.Interfaces;
using FileStorage.Infrastructure.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

/// <summary>
/// Redis TTL-backed temporary links. Stored in the Token database index
/// (separate from the DB metadata cache). Tokens expire via Redis TTL — an
/// expired link simply returns null with zero DB traffic.
/// </summary>
public sealed class TempLinkStore : ITempLinkStore
{
    private readonly IConnectionMultiplexer _multiplexer;
    private readonly int _database;

    public TempLinkStore(IConnectionMultiplexer multiplexer, IOptions<RedisOptions> options)
    {
        _multiplexer = multiplexer;
        _database = options.Value.TokenDatabase;
    }

    private IDatabase Db => _multiplexer.GetDatabase(_database);

    public async Task<string> CreateAsync(Guid fileId, int ttlSeconds, CancellationToken cancellationToken = default)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24)); // 48 hex chars, high entropy
        await Db.StringSetAsync(CacheKeys.TempLink(token), fileId.ToString("N"), TimeSpan.FromSeconds(ttlSeconds));
        return token;
    }

    public async Task<Guid?> ResolveAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var value = await Db.StringGetAsync(CacheKeys.TempLink(token));
        if (value.IsNullOrEmpty)
        {
            return null; // expired or never existed — Redis auto-removed it
        }

        return Guid.TryParse(value.ToString(), out var id) ? id : null;
    }
}