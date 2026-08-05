namespace FileStorage.Infrastructure.Storage;

using FileStorage.Application.Interfaces;
using FileStorage.Infrastructure.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

/// <summary>
/// Local-disk blob storage. Linux-compatible paths, server-generated file names
/// (GUID + detected extension), directory outside the web root.
/// </summary>
public sealed class DiskFileStorage : IFileStorage
{
    private readonly string _rootPath;
    private readonly ILogger<DiskFileStorage> _logger;

    public DiskFileStorage(IOptions<StorageOptions> options, ILogger<DiskFileStorage> logger)
    {
        _rootPath = options.Value.RootPath;
        _logger = logger;
    }

    public async Task<string> SaveAsync(Guid fileId, string extension, Stream content, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(_rootPath);

        var fileName = $"{fileId:N}.{extension.TrimStart('.')}";
        var fullPath = Path.Combine(_rootPath, fileName);

        await using var fileStream = new FileStream(
            fullPath,
            FileMode.CreateNew,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 81920,
            useAsync: true);

        await content.CopyToAsync(fileStream, cancellationToken);

        // Storage key stored in DB is just the file name — the root is resolved
        // by ResolvePath, keeping paths portable between environments.
        return fileName;
    }

    public Stream OpenRead(string storageKey)
    {
        var path = ResolvePath(storageKey)
            ?? throw new InvalidOperationException($"Invalid storage key '{storageKey}'.");

        return new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 81920,
            useAsync: true);
    }

    public string? ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey))
        {
            return null;
        }

        // Safety: storage keys are server-generated GUIDs; never allow traversal.
        var fileName = Path.GetFileName(storageKey);
        return Path.Combine(_rootPath, fileName);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (path is not null && File.Exists(path))
        {
            File.Delete(path);
            _logger.LogDebug("Deleted physical file {Path}.", path);
        }

        return Task.CompletedTask;
    }
}