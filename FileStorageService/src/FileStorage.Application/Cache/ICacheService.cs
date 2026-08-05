namespace FileStorage.Application.Cache;

/// <summary>
/// Cache abstraction used by the whole application layer.
/// Implementations are in Infrastructure (Redis in production,
/// in-memory/Null in tests and local dev without Redis).
/// </summary>
public interface ICacheService
{
    Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default);

    Task<string?> GetStringAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> GetBoolAsync(string key, CancellationToken cancellationToken = default);

    Task SetAsync<T>(string key, T value, TimeSpan ttl, CancellationToken cancellationToken = default);

    Task SetStringAsync(string key, string value, TimeSpan ttl, CancellationToken cancellationToken = default);

    Task RemoveAsync(params string[] keys);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Adds the key to the invalidation index for a file.</summary>
    Task AddToIndexAsync(string indexKey, string member, TimeSpan ttl, CancellationToken cancellationToken = default);

    /// <summary>Returns all members of a Redis SET (used for the file invalidation index).</summary>
    Task<IEnumerable<string>> GetSetMembersAsync(string key, CancellationToken cancellationToken = default);
}