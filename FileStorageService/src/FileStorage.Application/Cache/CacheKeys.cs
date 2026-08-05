namespace FileStorage.Application.Cache;

/// <summary>
/// Central place for all Redis key conventions.
///
/// Two separate concerns are kept apart (per employer requirement):
///   - <see cref="DbCachePrefix"/> : read-through cache of DB rows (metadata, membership)
///   - <see cref="TempLinkPrefix"/> : time-limited temp links backed purely by Redis TTL
///
/// They are also physically separated in Redis using different DB indexes
/// (0 = DB cache, 1 = temp links) configured in Infrastructure.
/// </summary>
public static class CacheKeys
{
    public const string DbCachePrefix = "file:meta:";
    public const string TempLinkPrefix = "temp:link:";
    public const string GroupMembershipPrefix = "group:member:";
    public const string StaticFilePrefix = "file:static:";

    /// <summary>Invaldiation index: fileId -> the set of cache keys used by that file.</summary>
    public const string FileKeysIndexPrefix = "file:keys:";

    public static string FileMetaByCode(string shortCode) => $"{DbCachePrefix}code:{shortCode}";

    public static string FileMetaByName(string friendlyName) => $"{DbCachePrefix}name:{friendlyName}";

    public static string FileMetaById(Guid fileId) => $"{DbCachePrefix}id:{fileId:N}";

    public static string GroupMembership(Guid groupId, Guid userId) => $"{GroupMembershipPrefix}{groupId:N}:{userId:N}";

    public static string StaticFileByCode(string shortCode) => $"{StaticFilePrefix}{shortCode}";

    public static string TempLink(string token) => $"{TempLinkPrefix}{token}";

    public static string FileKeysIndex(Guid fileId) => $"{FileKeysIndexPrefix}{fileId:N}";
}