namespace FileStorage.Common.Storage;

/// <summary>Lightweight magic-number file type checker (works on any OS, no native deps).</summary>
public static class MagicNumberChecker
{
    // Signature => (Extension, MimeType). First byte sequence wins.
    private static readonly (byte[] Signature, string Extension, string MimeType)[] Signatures =
    {
        (new byte[] { 0x25, 0x50, 0x44, 0x46 }, "pdf", "application/pdf"),
        (new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "zip", "application/zip"), // zip + docx/xlsx/pptx
        (new byte[] { 0xFF, 0xD8, 0xFF }, "jpg", "image/jpeg"),
        (new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "png", "image/png"),
        (new byte[] { 0x47, 0x49, 0x46, 0x38 }, "gif", "image/gif"),
        (new byte[] { 0x42, 0x4D }, "bmp", "image/bmp"),
        (new byte[] { 0x00, 0x00, 0x01, 0x00 }, "ico", "image/x-icon"),
        (new byte[] { 0x57, 0x45, 0x42, 0x50 }, "webp", "image/webp"),
        (new byte[] { 0x52, 0x49, 0x46, 0x46 }, "webp", "image/webp"), // RIF.. + WEBP further out
        (new byte[] { 0x49, 0x44, 0x33 }, "mp3", "audio/mpeg"),
        (new byte[] { 0xFF, 0xFB }, "mp3", "audio/mpeg"),
        (new byte[] { 0x4F, 0x67, 0x67, 0x53 }, "ogg", "audio/ogg"),
        (new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, "webm", "video/webm"),
        (new byte[] { 0x52, 0x49, 0x46, 0x46 }, "wav", "audio/wav"),
        (new byte[] { 0x66, 0x4C, 0x61, 0x43 }, "flac", "audio/flac"),
        (new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 }, "mp4", "video/mp4"),
        (new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 }, "mp4", "video/mp4"),
        (new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, "mkv", "video/x-matroska"),
        (new byte[] { 0x5B }, "txt", "text/plain"),
        (new byte[] { 0xEF, 0xBB, 0xBF }, "txt", "text/plain"),
        (new byte[] { 0x2F, 0x2A }, "txt", "text/plain"),
        (new byte[] { 0x7B }, "json", "application/json"),
        (new byte[] { 0x3C }, "xml", "text/xml"),
        (new byte[] { 0x3C, 0x3F, 0x78, 0x6D, 0x6C }, "xml", "text/xml"),
        (new byte[] { 0x3C, 0x21, 0x44, 0x4F, 0x43, 0x54, 0x59, 0x50, 0x45 }, "html", "text/html"),
        (new byte[] { 0xFE, 0xFF }, "txt", "text/plain"),
        (new byte[] { 0xFF, 0xFE }, "txt", "text/plain"),
        (new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }, "doc", "application/msword"), // OLE2 compound
        (new byte[] { 0x7F, 0x45, 0x4C, 0x46 }, "bin", "application/octet-stream"), // ELF
        (new byte[] { 0x25, 0x21 }, "bin", "application/octet-stream"), // PostScript/PDF-ish
        (new byte[] { 0x1F, 0x8B }, "gz", "application/gzip"),
        (new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 }, "rar", "application/vnd.rar"),
        (new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }, "7z", "application/x-7z-compressed"),
        (new byte[] { 0x50, 0x4B, 0x05, 0x06 }, "zip", "application/zip"), // empty zip
        (new byte[] { 0x53, 0x51, 0x4C, 0x69, 0x74, 0x65, 0x20, 0x66, 0x6F, 0x72, 0x6D, 0x61, 0x74, 0x20, 0x33 }, "sqlite", "application/vnd.sqlite3"),
        (new byte[] { 0x4C, 0x61, 0x76, 0x63, 0x35, 0x38, 0x2E, 0x31, 0x36 }, "iso", "application/x-iso9660-image"),
        (new byte[] { 0xFD, 0x37, 0x7A, 0x58, 0x5A, 0x00 }, "xz", "application/x-xz"),
    };

    /// <summary>
    /// Inspects the first bytes of a stream and returns the detected
    /// (extension, mimeType). Returns null when nothing matches
    /// (caller can then decide to reject the upload).
    /// </summary>
    public static DetectedType? Detect(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var buffer = new byte[16];
        var read = stream.Read(buffer, 0, buffer.Length);

        if (read <= 0)
        {
            return null;
        }

        foreach (var signature in Signatures)
        {
            if (SignatureMatches(buffer, read, signature.Signature))
            {
                return new DetectedType(signature.Extension, signature.MimeType);
            }
        }

        return null;
    }

    private static bool SignatureMatches(byte[] buffer, int read, byte[] signature)
    {
        if (signature.Length > read)
        {
            return false;
        }

        for (var i = 0; i < signature.Length; i++)
        {
            if (buffer[i] != signature[i])
            {
                return false;
            }
        }

        return true;
    }
}

public readonly record struct DetectedType(string Extension, string MimeType);