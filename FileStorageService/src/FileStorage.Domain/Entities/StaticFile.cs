namespace FileStorage.Domain.Entities;

/// <summary>
/// Static files live in a dedicated table (per employer requirement).
/// They are served with strong HTTP cache headers so Nginx can cache them
/// and the backend is never hit for repeated reads.
/// </summary>
public class StaticFile
{
    public Guid Id { get; set; }
    public string ShortCode { get; set; } = null!;
    public string StoragePath { get; set; } = null!;
    public string PhysicalName { get; set; } = null!;
    public string MimeType { get; set; } = null!;
    public long SizeBytes { get; set; }
    public int CacheControlSeconds { get; set; } = 86400;
    public DateTime CreatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}