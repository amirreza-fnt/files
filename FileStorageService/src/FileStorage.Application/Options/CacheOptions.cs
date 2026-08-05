namespace FileStorage.Application.Options;

/// <summary>Metadata cache TTLs (seconds). Values can be overridden in appsettings.</summary>
public sealed class CacheOptions
{
    public const string SectionName = "Cache";

    /// <summary>TTL for file metadata cache entries (seconds).</summary>
    public int FileMetadataTtlSeconds { get; set; } = 300;

    /// <summary>TTL for group membership cache entries (seconds) — shorter because access may change.</summary>
    public int GroupMembershipTtlSeconds { get; set; } = 60;

    /// <summary>TTL for static-file metadata cache entries (seconds) — long-lived.</summary>
    public int StaticFileTtlSeconds { get; set; } = 86400;

    /// <summary>TTL for the per-file invalidation index (must outlive metadata TTL).</summary>
    public int FileKeysIndexTtlSeconds { get; set; } = 604800; // 7 days

    public TimeSpan FileMetadataTtl => TimeSpan.FromSeconds(FileMetadataTtlSeconds);
    public TimeSpan GroupMembershipTtl => TimeSpan.FromSeconds(GroupMembershipTtlSeconds);
    public TimeSpan StaticFileTtl => TimeSpan.FromSeconds(StaticFileTtlSeconds);
    public TimeSpan FileKeysIndexTtl => TimeSpan.FromSeconds(FileKeysIndexTtlSeconds);
}