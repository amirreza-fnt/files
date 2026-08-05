namespace FileStorage.Application.Interfaces;

/// <summary>
/// Physical blob storage. Paths are Linux-compatible. Implementations:
/// local disk (main) or object storage for scale-out later.
/// </summary>
public interface IFileStorage
{
    /// <summary>Saves a stream to storage and returns the physical storage key/path.</summary>
    Task<string> SaveAsync(Guid fileId, string extension, Stream content, CancellationToken cancellationToken = default);

    /// <summary>Opens a read stream for a stored file.</summary>
    Stream OpenRead(string storageKey);

    /// <summary>Gets the absolute filesystem path (local disk implementation). Returns null if file absent.</summary>
    string? ResolvePath(string storageKey);

    /// <summary>Deletes the physical file. Idempotent — no-op if the key is already gone.</summary>
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
}