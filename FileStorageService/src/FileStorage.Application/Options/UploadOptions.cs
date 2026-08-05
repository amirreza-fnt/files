namespace FileStorage.Application.Options;

/// <summary>Upload constraints read from configuration (appsettings).</summary>
public sealed class UploadOptions
{
    public const string SectionName = "Upload";

    /// <summary>Whitelisted extensions WITHOUT the leading dot, lower-case.</summary>
    public HashSet<string> AllowedExtensions { get; set; } = new(StringComparer.OrdinalIgnoreCase)
    {
        "jpg", "jpeg", "png", "gif", "webp", "bmp", "pdf", "txt", "doc", "docx",
        "xls", "xlsx", "ppt", "pptx", "zip", "rar", "7z", "mp3", "mp4", "webm", "ogg", "wav"
    };

    public long MaxSizeBytes { get; set; } = 100L * 1024 * 1024; // 100 MB default

    /// <summary>TTL of the prepare-upload token.</summary>
    public int PrepareTokenTtlSeconds { get; set; } = 300;
}