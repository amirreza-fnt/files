namespace FileStorage.Application.Dtos;

using FileStorage.Domain.Enums;

/// <summary>Step 1 of the two-step upload: request metadata before any bytes flow.</summary>
public sealed record PrepareUploadRequest(
    string FileName,
    string Title,
    long SizeBytes,
    AccessType AccessType,
    string? Description = null,
    Guid? GroupId = null);

public sealed record PrepareUploadResponse(
    string UploadToken,
    int UploadTokenTtlSeconds,
    string[] AllowedExtensions,
    long MaxSizeBytes);

public sealed record UploadResponse(
    Guid FileId,
    string ShortCode,
    string FriendlyName,
    string Title,
    string? Description,
    string Url,
    string OriginalFileName,
    long SizeBytes);

public sealed record FileInfoResponse(
    Guid Id,
    string ShortCode,
    string FriendlyName,
    string Title,
    string? Description,
    string OriginalFileName,
    string Extension,
    string MimeType,
    long SizeBytes,
    AccessType AccessType,
    Guid? OwnerUserId,
    Guid? GroupId,
    DateTime CreatedAtUtc);

/// <summary>Result of a read operation — everything needed to stream the file.</summary>
public sealed record FileReadResult(
    FileInfoResponse Metadata,
    Func<Stream> OpenRead,
    string VersionToken);

/// <summary>Result for static files — includes strong cache headers for HTTP-level caching.</summary>
public sealed record StaticFileReadResult(
    string ShortCode,
    string MimeType,
    string OriginalFileName,
    long SizeBytes,
    int CacheControlSeconds,
    string VersionToken,
    Func<Stream> OpenRead);

/// <summary>
/// Serializable projection of a FileItem used as the Redis payload for
/// metadata reads (Cache-Aside). Kept free of EF navigation/byte[] raw fields
/// so it can be safely serialized.
/// </summary>
public sealed record CachedFileMeta(
    Guid Id,
    string ShortCode,
    string FriendlyName,
    string Title,
    string? Description,
    string OriginalFileName,
    string Extension,
    string MimeType,
    long SizeBytes,
    string StoragePath,
    Domain.Enums.AccessType AccessType,
    Guid? OwnerUserId,
    Guid? GroupId,
    bool IsDeleted,
    DateTime CreatedAtUtc,
    string VersionToken);