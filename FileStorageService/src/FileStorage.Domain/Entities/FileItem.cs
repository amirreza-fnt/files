namespace FileStorage.Domain.Entities;

/// <summary>
/// Main storage entity. Soft-deletable. Metadata is cached in Redis
/// (Cache-Aside) because it is the hottest read in the system.
/// </summary>
public class FileItem
{
    private FileItem() { }

    public FileItem(
        Guid id,
        string friendlyName,
        string shortCode,
        string originalFileName,
        string extension,
        string mimeType,
        long sizeBytes,
        string storagePath,
        Enums.AccessType accessType,
        Guid? ownerUserId,
        Guid? groupId,
        byte[] rowVersion,
        DateTime createdAtUtc)
    {
        Id = id;
        FriendlyName = friendlyName;
        ShortCode = shortCode;
        OriginalFileName = originalFileName;
        Extension = extension;
        MimeType = mimeType;
        SizeBytes = sizeBytes;
        StoragePath = storagePath;
        AccessType = accessType;
        OwnerUserId = ownerUserId;
        GroupId = groupId;
        RowVersion = rowVersion;
        CreatedAtUtc = createdAtUtc;
    }

    public Guid Id { get; set; }
    public string FriendlyName { get; set; }
    public string ShortCode { get; set; }
    public string OriginalFileName { get; set; }
    public string Extension { get; set; }
    public string MimeType { get; set; }
    public long SizeBytes { get; set; }
    public string StoragePath { get; set; }

    /// <summary>Physical on-disk file name (never the user supplied name).</summary>
    public string PhysicalName { get; set; } = null!;

    public Enums.AccessType AccessType { get; set; }
    public Guid? OwnerUserId { get; set; }
    public Guid? GroupId { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime? DeletedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>SQL rowversion used for optimistic concurrency + precise cache invalidation.</summary>
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();

    /// <summary>Concurrency/version watermark exposed to cache layer.</summary>
    public string VersionToken => Convert.ToBase64String(RowVersion);

    public void SoftDelete(DateTime utcNow)
    {
        if (IsDeleted)
        {
            return;
        }

        IsDeleted = true;
        DeletedAtUtc = utcNow;
    }
}