namespace FileStorage.Application.Services;

using FileStorage.Application.Dtos;
using FileStorage.Application.Interfaces;
using FileStorage.Application.Options;
using FileStorage.Common.Storage;
using FileStorage.Domain.Entities;
using Microsoft.Extensions.Options;

public interface IFileUploadService
{
    Task<PrepareUploadResponse> PrepareUploadAsync(PrepareUploadRequest request, CancellationToken cancellationToken = default);

    Task<UploadResponse> UploadAsync(string uploadToken, Stream content, CancellationToken cancellationToken = default);
}

public sealed class FileUploadService : IFileUploadService
{
    private readonly IUploadTokenStore _uploadTokenStore;
    private readonly IFileRepository _fileRepository;
    private readonly IFileMetadataCache _metadataCache;
    private readonly IFileStorage _fileStorage;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly UploadOptions _uploadOptions;
    private readonly CacheOptions _cacheOptions;

    public FileUploadService(
        IUploadTokenStore uploadTokenStore,
        IFileRepository fileRepository,
        IFileMetadataCache metadataCache,
        IFileStorage fileStorage,
        ICurrentUserAccessor currentUser,
        IOptions<UploadOptions> uploadOptions,
        IOptions<CacheOptions> cacheOptions)
    {
        _uploadTokenStore = uploadTokenStore;
        _fileRepository = fileRepository;
        _metadataCache = metadataCache;
        _fileStorage = fileStorage;
        _currentUser = currentUser;
        _uploadOptions = uploadOptions.Value;
        _cacheOptions = cacheOptions.Value;
    }

    public async Task<PrepareUploadResponse> PrepareUploadAsync(PrepareUploadRequest request, CancellationToken cancellationToken = default)
    {
        var extension = NormalizeExtension(request.FileName);

        if (string.IsNullOrEmpty(extension) || !_uploadOptions.AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException($"Extension '{extension}' is not allowed.");
        }

        if (request.SizeBytes > _uploadOptions.MaxSizeBytes)
        {
            throw new InvalidOperationException($"File exceeds the maximum allowed size of {_uploadOptions.MaxSizeBytes} bytes.");
        }

        if (request.AccessType == Domain.Enums.AccessType.GroupRestricted && !request.GroupId.HasValue)
        {
            throw new InvalidOperationException("GroupId is required for GroupRestricted access.");
        }

        var payload = new UploadTokenPayload(
            request.FileName,
            request.SizeBytes,
            request.AccessType,
            request.GroupId,
            _currentUser.UserId);

        var token = await _uploadTokenStore.CreateAsync(payload, _uploadOptions.PrepareTokenTtlSeconds, cancellationToken);

        return new PrepareUploadResponse(
            token,
            _uploadOptions.PrepareTokenTtlSeconds,
            _uploadOptions.AllowedExtensions.OrderBy(x => x).ToArray(),
            _uploadOptions.MaxSizeBytes);
    }

    public async Task<UploadResponse> UploadAsync(string uploadToken, Stream content, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(uploadToken))
        {
            throw new InvalidOperationException("UploadToken header is required.");
        }

        var payload = await _uploadTokenStore.ResolveAsync(uploadToken, cancellationToken)
            ?? throw new InvalidOperationException("Upload token is missing or expired. Call prepare-upload first.");

        // Fully buffer the body first so we can sniff the signature AND read the
        // bytes — works regardless of whether the source stream is seekable.
        using var buffer = new MemoryStream();
        await content.CopyToAsync(buffer, cancellationToken);

        if (buffer.Length <= 0)
        {
            throw new InvalidOperationException("Uploaded file is empty.");
        }

        if (buffer.Length > _uploadOptions.MaxSizeBytes)
        {
            throw new InvalidOperationException($"File exceeds the maximum allowed size of {_uploadOptions.MaxSizeBytes} bytes.");
        }

        buffer.Position = 0;
        var detected = MagicNumberChecker.Detect(buffer);
        if (detected is null)
        {
            throw new InvalidOperationException("Unable to detect the file type from its content (bad or unsupported magic number).");
        }

        // Validate the magic-number result against the whitelist (real file type, not just the name).
        if (!_uploadOptions.AllowedExtensions.Contains(detected.Value.Extension))
        {
            throw new InvalidOperationException($"Detected file type '{detected.Value.Extension}' is not allowed.");
        }

        var fileId = Guid.NewGuid();
        var shortCode = await CreateUniqueShortCodeAsync(cancellationToken);
        var friendlyName = await CreateUniqueFriendlyNameAsync(StripExtension(payload.FileName), shortCode, cancellationToken);

        // Save physical bytes under a server-generated name (never the user-supplied name).
        buffer.Position = 0;
        var storagePath = await _fileStorage.SaveAsync(fileId, detected.Value.Extension, buffer, cancellationToken);

        var now = DateTime.UtcNow;
        var file = new FileItem(
            fileId,
            friendlyName,
            shortCode,
            payload.FileName,
            detected.Value.Extension,
            detected.Value.MimeType,
            buffer.Length,
            storagePath,
            payload.AccessType,
            payload.OwnerUserId,
            payload.AccessType == Domain.Enums.AccessType.GroupRestricted ? payload.GroupId : null,
            Array.Empty<byte>(),
            now)
        {
            PhysicalName = Path.GetFileName(storagePath)
        };

        await _fileRepository.CreateAsync(file, cancellationToken);

        // Warm cache immediately — a read right after upload is very likely.
        await _metadataCache.WarmAsync(file, cancellationToken);

        return new UploadResponse(
            file.Id,
            file.ShortCode,
            file.FriendlyName,
            $"/i/{file.ShortCode}",
            file.OriginalFileName,
            file.SizeBytes);
    }

    private async Task<string> CreateUniqueShortCodeAsync(CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 10; attempt++)
        {
            var code = ShortCodeGenerator.Generate(5);
            if (await _fileRepository.IsShortCodeFreeAsync(code, cancellationToken))
            {
                return code;
            }
        }

        throw new InvalidOperationException("Unable to allocate a unique short code.");
    }

    private async Task<string> CreateUniqueFriendlyNameAsync(string baseName, string shortCode, CancellationToken cancellationToken)
    {
        var candidate = Slugify(baseName);
        if (string.IsNullOrEmpty(candidate))
        {
            candidate = "file";
        }

        if (await _fileRepository.IsFriendlyNameFreeAsync(candidate, cancellationToken))
        {
            return candidate;
        }

        for (var attempt = 0; attempt < 10; attempt++)
        {
            var tagged = $"{candidate}-{ShortCodeGenerator.Generate(4)}";
            if (await _fileRepository.IsFriendlyNameFreeAsync(tagged, cancellationToken))
            {
                return tagged;
            }
        }

        return $"{candidate}-{shortCode}";
    }

    private static string NormalizeExtension(string fileName)
        => Path.GetExtension(fileName)?.TrimStart('.').ToLowerInvariant() ?? string.Empty;

    private static string StripExtension(string fileName)
        => Path.GetFileNameWithoutExtension(fileName);

    private static string Slugify(string input)
    {
        var normalized = input.Trim().ToLowerInvariant();
        var chars = normalized
            .Select(c => char.IsLetterOrDigit(c) ? c : '-')
            .ToArray();
        var slug = new string(chars);
        slug = string.Join('-', slug.Split('-', StringSplitOptions.RemoveEmptyEntries));
        return slug.Length > 80 ? slug[..80] : slug;
    }
}