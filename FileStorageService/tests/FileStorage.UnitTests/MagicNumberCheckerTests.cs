using System.Text;
using FileStorage.Common.Storage;

namespace FileStorage.UnitTests;

public class MagicNumberCheckerTests
{
    [Theory]
    [InlineData(new byte[] { 0x25, 0x50, 0x44, 0x46, 0x2D }, "pdf", "application/pdf")]
    [InlineData(new byte[] { 0xFF, 0xD8, 0xFF, 0xE0 }, "jpg", "image/jpeg")]
    [InlineData(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }, "png", "image/png")]
    [InlineData(new byte[] { 0x47, 0x49, 0x46, 0x38, 0x39, 0x61 }, "gif", "image/gif")]
    [InlineData(new byte[] { 0x52, 0x61, 0x72, 0x21, 0x1A, 0x07 }, "rar", "application/vnd.rar")]
    [InlineData(new byte[] { 0x1F, 0x8B, 0x08 }, "gz", "application/gzip")]
    public void Detect_IdentifiesKnownSignatures(byte[] header, string expectedExt, string expectedMime)
    {
        using var stream = new MemoryStream(header);

        var detected = MagicNumberChecker.Detect(stream);

        Assert.NotNull(detected);
        Assert.Equal(expectedExt, detected!.Value.Extension);
        Assert.Equal(expectedMime, detected.Value.MimeType);
    }

    [Fact]
    public void Detect_ReturnsNull_ForUnknownSignature()
    {
        var unknown = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55 };
        using var stream = new MemoryStream(unknown);

        var detected = MagicNumberChecker.Detect(stream);

        Assert.Null(detected);
    }

    [Fact]
    public void Detect_ReturnsNull_ForEmptyStream()
    {
        using var stream = new MemoryStream();

        Assert.Null(MagicNumberChecker.Detect(stream));
    }

    [Fact]
    public void Detect_DoesNotMatchTruncatedSignature()
    {
        // Only 2 bytes of a 4-byte PDF signature.
        var truncated = new byte[] { 0x25, 0x50 };
        using var stream = new MemoryStream(truncated);

        Assert.Null(MagicNumberChecker.Detect(stream));
    }

    [Fact]
    public void Detect_DoesNotMatchInsideText()
    {
        var text = Encoding.UTF8.GetBytes("This is not a file signature. PDF bytes can appear mid-string.");
        using var stream = new MemoryStream(text);

        Assert.Null(MagicNumberChecker.Detect(stream));
    }
}