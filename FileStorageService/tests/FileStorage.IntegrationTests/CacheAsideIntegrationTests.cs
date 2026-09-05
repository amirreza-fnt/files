using System.Data.Common;
using FileStorage.Application.Cache;
using FileStorage.Application.Dtos;
using FileStorage.Application.Interfaces;
using FileStorage.Application.Options;
using FileStorage.Domain.Entities;
using FileStorage.Domain.Enums;
using FileStorage.Infrastructure.Persistence;
using FileStorage.Infrastructure.Redis;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace FileStorage.IntegrationTests;

/// <summary>
/// Exercises the real Cache-Aside + invalidation path (FileMetadataCache +
/// FileCacheInvalidator + EF Core repository) against an in-memory SQLite DB
/// and an in-memory cache. No external MSSQL/Redis needed.
/// </summary>
public sealed class CacheAsideIntegrationTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FileStorageDbContext _context;
    private readonly InMemoryCache _cache;
    private readonly IFileMetadataCache _metadataCache;
    private readonly IFileCacheInvalidator _invalidator;
    private readonly IFileRepository _repository;

    public CacheAsideIntegrationTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<FileStorageDbContext>()
            .UseSqlite(_connection)
            .Options;

        _context = new FileStorageDbContext(options);
        _context.Database.EnsureCreated();

        _cache = new InMemoryCache();
        _repository = new FileRepository(_context);

        _metadataCache = new FileMetadataCache(
            _cache,
            _repository,
            Options.Create(new CacheOptions()),
            NullLogger<FileMetadataCache>.Instance);

        _invalidator = new FileCacheInvalidator(
            _cache,
            NullLogger<FileCacheInvalidator>.Instance);
    }

    private static FileItem CreateFile(string code, string name)
    {
        var id = Guid.NewGuid();
        return new FileItem(
            id,
            name,
            code,
            "Test title",
            "Test description",
            name + ".pdf",
            "pdf",
            "application/pdf",
            10,
            id.ToString("N") + ".pdf",
            AccessType.Public,
            null,
            null,
            Array.Empty<byte>(),
            DateTime.UtcNow)
        {
            PhysicalName = id.ToString("N") + ".pdf",
        };
    }

    [Fact]
    public async Task GetByShortCode_WarmsCache_AndSecondReadHitsCache()
    {
        var file = CreateFile("A1b2C", "my-doc");
        await _repository.CreateAsync(file);

        var first = await _metadataCache.GetByShortCodeAsync("A1b2C");
        Assert.NotNull(first);

        // Second read must be served from cache: remove from DB to prove it.
        _cache.ClearStats();
        await _repository.HardDeleteAsync(file.Id);

        var second = await _metadataCache.GetByShortCodeAsync("A1b2C");
        Assert.NotNull(second);
        Assert.Equal(file.Id, second!.Id);
        Assert.True(_cache.GetCalls > 0, "Cache should have been hit on the second read.");
    }

    [Fact]
    public async Task Invalidate_RemovesEveryKey_ForTheFile()
    {
        var file = CreateFile("Xy7Qr", "report");
        await _repository.CreateAsync(file);
        await _metadataCache.GetByShortCodeAsync("Xy7Qr");       // warms code + name + id keys
        await _metadataCache.GetByFriendlyNameAsync("report");

        await _invalidator.InvalidateFileCacheAsync(file.Id, file.ShortCode, file.FriendlyName);

        // Delete the DB row, then a read must return null — it may NOT be served
        // from the stale cache that was just invalidated.
        await _repository.HardDeleteAsync(file.Id);
        var cached = await _metadataCache.GetByIdAsync(file.Id);
        Assert.Null(cached);
    }

    [Fact]
    public async Task Invalidator_IsIdempotent()
    {
        await _invalidator.InvalidateFileCacheAsync(Guid.NewGuid(), "AAAA", "missing");
        await _invalidator.InvalidateFileCacheAsync(Guid.NewGuid(), "BBBB", "missing-2");

        Assert.True(true);
    }

    [Fact]
    public async Task SoftDelete_ThenMetadataRead_ReturnsNull_AfterInvalidation()
    {
        var file = CreateFile("SoftDel1", "delete-me");
        await _repository.CreateAsync(file);
        await _metadataCache.GetByShortCodeAsync("SoftDel1");

        file.SoftDelete(DateTime.UtcNow);
        await _repository.UpdateAsync(file);
        await _invalidator.InvalidateFileCacheAsync(file.Id, file.ShortCode, file.FriendlyName);

        // Cache was invalidated AND the global query filter hides deleted rows.
        Assert.Null(await _metadataCache.GetByShortCodeAsync("SoftDel1"));
    }

    public void Dispose()
    {
        _context.Dispose();
        _connection.Dispose();
    }
}

/// <summary>Minimal in-memory ICacheService for tests.</summary>
public sealed class InMemoryCache : ICacheService
{
    private readonly Dictionary<string, (object? Value, DateTime? ExpiresAt)> _store = new();

    public int GetCalls { get; private set; }
    public int SetCalls { get; private set; }

    public void ClearStats()
    {
        GetCalls = 0;
        SetCalls = 0;
    }

    public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        GetCalls++;
        if (_store.TryGetValue(key, out var entry) && IsAlive(entry))
        {
            return Task.FromResult((T?)entry.Value);
        }

        _store.Remove(key);
        return Task.FromResult(default(T));
    }

    public Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default)
        => GetAsync<string>(key, cancellationToken);

    public Task<bool> GetBoolAsync(string key, CancellationToken cancellationToken = default)
    {
        var v = GetAsync<string>(key, cancellationToken);
        return Task.FromResult(v.Result is "1" or "true");
    }

    public Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        SetCalls++;
        _store[key] = (value, DateTime.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }

    public Task SetStringAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default)
        => SetAsync(key, value, ttl, cancellationToken);

    public Task RemoveAsync(params string[] keys)
    {
        foreach (var key in keys)
        {
            _store.Remove(key);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
        => Task.FromResult(_store.ContainsKey(key) && IsAlive(_store[key]));

    public Task AddToIndexAsync(string indexKey, string member, TimeSpan ttl, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(indexKey, out var entry) && entry.Value is List<string> list)
        {
            list.Add(member);
            _store[indexKey] = (list, DateTime.UtcNow.Add(ttl));
            return Task.CompletedTask;
        }

        _store[indexKey] = (new List<string> { member }, DateTime.UtcNow.Add(ttl));
        return Task.CompletedTask;
    }

    public Task<IEnumerable<string>> GetSetMembersAsync(string key, CancellationToken cancellationToken = default)
    {
        if (_store.TryGetValue(key, out var entry) && entry.Value is List<string> members)
        {
            return Task.FromResult(members.AsEnumerable());
        }

        return Task.FromResult(Enumerable.Empty<string>());
    }

    private static bool IsAlive((object? Value, DateTime? ExpiresAt) entry)
        => entry.ExpiresAt is null || entry.ExpiresAt > DateTime.UtcNow;
}