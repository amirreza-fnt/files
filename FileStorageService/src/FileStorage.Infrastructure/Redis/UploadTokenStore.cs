namespace FileStorage.Infrastructure.Redis;

using System.Security.Cryptography;
using FileStorage.Application.Dtos;
using FileStorage.Application.Interfaces;
using FileStorage.Infrastructure.Options;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

/// <summary>
/// Short-lived prepare-upload tokens (Redis TTL, Token database index).
/// The two-step upload contract requires a valid token before bytes are accepted.
/// </summary>
public sealed class UploadTokenStore : IUploadTokenStore
{
    private const string Prefix = "upload:token:";

    private readonly IConnectionMultiplexer _multiplexer;
    private readonly int _database;

    public UploadTokenStore(IConnectionMultiplexer multiplexer, IOptions<RedisOptions> options)
    {
        _multiplexer = multiplexer;
        _database = options.Value.TokenDatabase;
    }

    private IDatabase Db => _multiplexer.GetDatabase(_database);

    public async Task<string> CreateAsync(UploadTokenPayload payload, int ttlSeconds, CancellationToken cancellationToken = default)
    {
        var token = Convert.ToHexString(RandomNumberGenerator.GetBytes(24));
        var json = RedisJson.Serialize(payload);
        await Db.StringSetAsync(Prefix + token, json, TimeSpan.FromSeconds(ttlSeconds));
        return token;
    }

    public async Task<UploadTokenPayload?> ResolveAsync(string token, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        var json = await Db.StringGetAsync(Prefix + token);
        if (json.IsNullOrEmpty)
        {
            return null;
        }

        return RedisJson.Deserialize<UploadTokenPayload>(json!);
    }
}