namespace FileStorage.Infrastructure.Options;

/// <summary>Physical storage configuration.</summary>
public sealed class StorageOptions
{
    public const string SectionName = "Storage";

    /// <summary>
    /// Root directory for blobs. Linux default (outside the web root).
    /// The systemd user must have write access here.
    /// </summary>
    public string RootPath { get; set; } = "/var/lib/filestorage";
}

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    /// <summary>StackExchange.Redis connection string (e.g. host:port,password=...).</summary>
    public string ConnectionString { get; set; } = "localhost:6379";

    /// <summary>Redis logical database used for the read-through DB cache (file:meta:*, group:member:*, ...).</summary>
    public int DbCacheDatabase { get; set; } = 0;

    /// <summary>Redis logical database used for TTL-backed tokens (temp:link:*, upload:token:*).</summary>
    public int TokenDatabase { get; set; } = 1;
}