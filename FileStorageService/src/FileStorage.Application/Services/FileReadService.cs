namespace FileStorage.Application.Services;

using FileStorage.Application.Cache;
using FileStorage.Application.Dtos;
using FileStorage.Application.Interfaces;
using FileStorage.Domain.Enums;

/// <summary>
/// Read orchestration. Every metadata lookup goes through
/// <see cref="IFileMetadataCache"/> (Redis Cache-Aside) before MSSQL.
/// </summary>
public interface IFileReadService
{
    /// <summary>Reads a file by short code (<c>/i/CODE</c>). Public or token-protected.</summary>
    Task<FileReadResult?> ReadByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);

    /// <summary>Reads a public file by friendly name (<c>/f/NAME</c>).</summary>
    Task<FileReadResult?> ReadByFriendlyNameAsync(string friendlyName, CancellationToken cancellationToken = default);

    /// <summary>Reads a group-restricted file (<c>/g/CODE</c>) with a live membership check.</summary>
    Task<FileReadResult?> ReadByGroupAsync(string shortCode, Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Reads a temporary file using only the Redis TTL token (<c>/t/TOKEN</c>). No DB query.</summary>
    Task<FileReadResult?> ReadByTempTokenAsync(string token, CancellationToken cancellationToken = default);

    /// <summary>Reads a static file (<c>/c/CODE</c>) with strong HTTP cache headers.</summary>
    Task<StaticFileReadResult?> ReadStaticAsync(string shortCode, CancellationToken cancellationToken = default);

    /// <summary>Gets file info for admin panel (<c>/api/files/{{id}}</c>).</summary>
    Task<FileInfoResponse?> GetInfoAsync(Guid fileId, CancellationToken cancellationToken = default);
}

public sealed class FileReadService : IFileReadService
{
    private readonly IFileMetadataCache _metadataCache;
    private readonly IFileAuthorizationService _authorization;
    private readonly IFileGroupRepository _groupRepository;
    private readonly IStaticFileRepository _staticFileRepository;
    private readonly ITempLinkStore _tempLinkStore;
    private readonly IFileStorage _fileStorage;
    private readonly ICurrentUserAccessor _currentUser;

    public FileReadService(
        IFileMetadataCache metadataCache,
        IFileAuthorizationService authorization,
        IFileGroupRepository groupRepository,
        IStaticFileRepository staticFileRepository,
        ITempLinkStore tempLinkStore,
        IFileStorage fileStorage,
        ICurrentUserAccessor currentUser)
    {
        _metadataCache = metadataCache;
        _authorization = authorization;
        _groupRepository = groupRepository;
        _staticFileRepository = staticFileRepository;
        _tempLinkStore = tempLinkStore;
        _fileStorage = fileStorage;
        _currentUser = currentUser;
    }

    public async Task<FileReadResult?> ReadByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var meta = await _metadataCache.GetByShortCodeAsync(shortCode, cancellationToken);
        if (meta is null)
        {
            return null;
        }

        if (!_authorization.CanRead(meta, _currentUser.UserId, _currentUser.IsAdmin))
        {
            return null;
        }

        return BuildResult(meta);
    }

    public async Task<FileReadResult?> ReadByFriendlyNameAsync(string friendlyName, CancellationToken cancellationToken = default)
    {
        var meta = await _metadataCache.GetByFriendlyNameAsync(friendlyName, cancellationToken);
        if (meta is null || !_authorization.CanReadPublic(meta))
        {
            return null;
        }

        return BuildResult(meta);
    }

    public async Task<FileReadResult?> ReadByGroupAsync(string shortCode, Guid userId, CancellationToken cancellationToken = default)
    {
        var meta = await _metadataCache.GetByShortCodeAsync(shortCode, cancellationToken);
        if (meta is null || meta.AccessType != AccessType.GroupRestricted)
        {
            return null;
        }

        if (meta.OwnerUserId == userId || _currentUser.IsAdmin)
        {
            return BuildResult(meta);
        }

        if (!meta.GroupId.HasValue)
        {
            return null;
        }

        var isMember = await _groupRepository.IsMemberAsync(meta.GroupId.Value, userId, cancellationToken);
        return isMember ? BuildResult(meta) : null;
    }

    public async Task<FileReadResult?> ReadByTempTokenAsync(string token, CancellationToken cancellationToken = default)
    {
        // Redis-only resolution — expired tokens vanish automatically (TTL), no DB hit.
        var fileId = await _tempLinkStore.ResolveAsync(token, cancellationToken);
        if (!fileId.HasValue)
        {
            return null;
        }

        var meta = await _metadataCache.GetByIdAsync(fileId.Value, cancellationToken);
        if (meta is null || meta.IsDeleted)
        {
            return null;
        }

        return BuildResult(meta);
    }

    public async Task<StaticFileReadResult?> ReadStaticAsync(string shortCode, CancellationToken cancellationToken = default)
    {
        var file = await _staticFileRepository.GetByShortCodeAsync(shortCode, cancellationToken);
        if (file is null)
        {
            return null;
        }

        return new StaticFileReadResult(
            file.ShortCode,
            file.MimeType,
            file.ShortCode + Path.GetExtension(file.StoragePath),
            file.SizeBytes,
            file.CacheControlSeconds,
            Convert.ToBase64String(file.RowVersion),
            () => _fileStorage.OpenRead(file.StoragePath));
    }

    public async Task<FileInfoResponse?> GetInfoAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        var meta = await _metadataCache.GetByIdAsync(fileId, cancellationToken);
        if (meta is null)
        {
            return null;
        }

        return ToInfo(meta);
    }

    private FileReadResult BuildResult(CachedFileMeta meta)
    {
        var info = ToInfo(meta);
        return new FileReadResult(info, () => _fileStorage.OpenRead(meta.StoragePath), meta.VersionToken);
    }

    private static FileInfoResponse ToInfo(CachedFileMeta meta)
        => new(
            meta.Id,
            meta.ShortCode,
            meta.FriendlyName,
            meta.OriginalFileName,
            meta.Extension,
            meta.MimeType,
            meta.SizeBytes,
            meta.AccessType,
            meta.OwnerUserId,
            meta.GroupId,
            meta.CreatedAtUtc);
}