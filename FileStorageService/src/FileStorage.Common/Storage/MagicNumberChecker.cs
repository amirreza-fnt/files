namespace FileStorage.Common.Storage;

/// <summary>Lightweight magic-number file type checker (works on any OS, no native deps).</summary>
public static class MagicNumberChecker
{
    // Order matters for simple prefixes; RIFF containers are handled specially below.
    private static readonly (byte[] Signature, string Extension, string MimeType)[] Signatures =
    {
        (new byte[] { 0x25, 0x50, 0x44, 0x46 }, "pdf", "application/pdf"),
        (new byte[] { 0x50, 0x4B, 0x03, 0x04 }, "zip", "application/zip"),
        (new byte[] { 0xFF, 0xD8, 0xFF }, "jpg", "image/jpeg"),
        (new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "png", "image/png"),
        (new byte[] { 0x47, 0x49, 0x46, 0x38 }, "gif", "image/gif"),
        (new byte[] { 0x42, 0x4D }, "bmp", "image/bmp"),
        (new byte[] { 0x00, 0x00, 0x01, 0x00 }, "ico", "image/x-icon"),
        (new byte[] { 0x49, 0x44, 0x33 }, "mp3", "audio/mpeg"),
        (new byte[] { 0xFF, 0xFB }, "mp3", "audio/mpeg"),
        (new byte[] { 0x4F, 0x67, 0x67, 0x53 }, "ogg", "audio/ogg"),
        (new byte[] { 0x1A, 0x45, 0xDF, 0xA3 }, "webm", "video/webm"),
        (new byte[] { 0x66, 0x4C, 0x61, 0x43 }, "flac", "audio/flac"),
        (new byte[] { 0x00, 0x00, 0x00, 0x18, 0x66, 0x74, 0x79, 0x70 }, "mp4", "video/mp4"),
        (new byte[] { 0x00, 0x00, 0x00, 0x20, 0x66, 0x74, 0x79, 0x70 }, "mp4", "video/mp4"),
        (new byte[] { 0x5B }, "txt", "text/plain"),
        (new byte[] { 0xEF, 0xBB, 0xBF }, "txt", "text/plain"),
        (new byte[] { 0x2F, 0x2A }, "txt", "text/plain"),
        (new byte[] { 0x7B }, "json", "application/json"),
        (new byte[] { 0x3C, 0x3F, 0x78, 0x6D, 0x6C }, "xml", "text/xml"),
        (new byte[] { 0x3C, 0x21, 0x44, 0x4F, 0x43, 0x54, 0x59, 0x50, 0x45 }, "html", "text/html"),
        (new byte[] { 0x3C }, "xml", "text/xml"),
        (new byte[] { 0xFE, 0xFF }, "txt", "text/plain"),
        (new byte[] { 0xFF, 0xFE }, "txt", "text/plain"),
        (new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }, "doc", "application/msword"),
        (new byte[] { 0x1F, 0x8B }, "gz", "application/gzip"),
        (new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 }, "rar", "application/vnd.rar"),
        (new byte[] { 0x37, 0x7A, 0xBC, 0xAF, 0x27, 0x1C }, "7z", "application/x-7z-compressed"),
        (new byte[] { 0x50, 0x4B, 0x05, 0x06 }, "zip", "application/zip"),
    };

    public static DetectedType? Detect(Stream stream)
    {
        ArgumentNullException.ThrowIfNull(stream);

        var buffer = new byte[16];
        var read = stream.Read(buffer, 0, buffer.Length);

        if (read <= 0)
        {
            return null;
        }

        // RIFF containers: distinguish WAVE vs WEBP (and AVI) via bytes 8..11
        if (read >= 12
            && buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46)
        {
            var form = System.Text.Encoding.ASCII.GetString(buffer, 8, 4);
            if (form == "WAVE")
            {
                return new DetectedType("wav", "audio/wav");
            }

            if (form == "WEBP")
            {
                return new DetectedType("webp", "image/webp");
            }

            if (form == "AVI ")
            {
                return new DetectedType("avi", "video/x-msvideo");
            }
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
