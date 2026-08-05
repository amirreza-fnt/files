namespace FileStorage.Common.Storage;

using System.Security.Cryptography;

/// <summary>
/// Base62 short-code generator. Produces URL-safe codes of a fixed length
/// from cryptographically random data. Uniqueness is enforced by the caller
/// (check + retry) before insert — see UploadService.
/// </summary>
public static class ShortCodeGenerator
{
    private const string Alphabet = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    /// <summary>
    /// Generates a random Base62 code.
    /// </summary>
    /// <param name="length">Target length (4-6 recommended).</param>
    /// <remarks>
    /// Length 5 over 62 symbols gives ~62^5 ≈ 916M combinations, which is
    /// effectively collision-free at platform scale when combined with a
    /// uniqueness check + retry loop.
    /// </remarks>
    public static string Generate(int length = 5)
    {
        if (length < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(length));
        }

        var bytes = RandomNumberGenerator.GetBytes(length);
        var chars = new char[length];

        for (var i = 0; i < length; i++)
        {
            chars[i] = Alphabet[bytes[i] % Alphabet.Length];
        }

        return new string(chars);
    }
}