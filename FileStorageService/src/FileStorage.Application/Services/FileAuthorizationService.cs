namespace FileStorage.Application.Services;

using FileStorage.Application.Dtos;
using FileStorage.Domain.Enums;

/// <summary>Central authorization decisions for file access and management.</summary>
public interface IFileAuthorizationService
{
    /// <summary>True if a public file may be served to anyone.</summary>
    bool CanReadPublic(CachedFileMeta meta);

    /// <summary>
    /// Non-group authorization check. Group membership is verified separately
    /// (async, cached) by the read service.
    /// </summary>
    bool CanRead(CachedFileMeta meta, Guid? userId, bool isAdmin);

    bool CanManage(Guid? ownerUserId, Guid? userId, bool isAdmin);
}

public sealed class FileAuthorizationService : IFileAuthorizationService
{
    public bool CanReadPublic(CachedFileMeta meta)
        => !meta.IsDeleted && meta.AccessType == AccessType.Public;

    public bool CanRead(CachedFileMeta meta, Guid? userId, bool isAdmin)
    {
        if (meta.IsDeleted)
        {
            return false;
        }

        return meta.AccessType switch
        {
            AccessType.Public => true,
            AccessType.TokenProtected => isAdmin || (userId.HasValue && userId == meta.OwnerUserId),
            AccessType.GroupRestricted => isAdmin || (userId.HasValue && userId == meta.OwnerUserId),
            // Temporary files are only reachable through the /t/{token} endpoint (Redis TTL),
            // not through a code-based endpoint.
            AccessType.Temporary => false,
            AccessType.Static => isAdmin || (userId.HasValue && userId == meta.OwnerUserId),
            _ => false,
        };
    }

    public bool CanManage(Guid? ownerUserId, Guid? userId, bool isAdmin)
        => isAdmin || (userId.HasValue && userId == ownerUserId);
}