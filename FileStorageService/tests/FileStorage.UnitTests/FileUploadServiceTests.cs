using System.Text;
using FileStorage.Application.Dtos;
using FileStorage.Application.Interfaces;
using FileStorage.Application.Options;
using FileStorage.Application.Services;
using FileStorage.Domain.Entities;
using FileStorage.Domain.Enums;
using Microsoft.Extensions.Options;
using Moq;

namespace FileStorage.UnitTests;

public class FileUploadServiceTests
{
    private readonly Mock<IFileRepository> _repository = new();
    private readonly Mock<IFileMetadataCache> _metadataCache = new();
    private readonly Mock<IFileStorage> _fileStorage = new();
    private readonly Mock<ICurrentUserAccessor> _currentUser = new();

    private static UploadOptions UploadOptions() => new()
    {
        MaxSizeBytes = 1024 * 1024,
        PrepareTokenTtlSeconds = 300,
        AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "png", "pdf", "zip" },
    };

    private FileUploadService Build(
        IUploadTokenStore? tokenStore = null,
        UploadOptions? uploadOptions = null)
    {
        tokenStore ??= new FakeTokenStore();
        uploadOptions ??= UploadOptions();

        return new FileUploadService(
            tokenStore,
            _repository.Object,
            _metadataCache.Object,
            _fileStorage.Object,
            _currentUser.Object,
            Options.Create(uploadOptions),
            Options.Create(new CacheOptions()));
    }

    [Fact]
    public async Task PrepareUpload_RejectsDisallowedExtension()
    {
        var sut = Build();
        var request = new PrepareUploadRequest("evil.exe", 100, AccessType.Public);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PrepareUploadAsync(request));
    }

    [Fact]
    public async Task PrepareUpload_RejectsOversizeFile()
    {
        var sut = Build();
        var request = new PrepareUploadRequest("big.png", 10L * 1024 * 1024, AccessType.Public);

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.PrepareUploadAsync(request));
    }

    [Fact]
    public async Task PrepareUpload_ReturnsTokenForValidRequest()
    {
        var sut = Build();
        var request = new PrepareUploadRequest("photo.png", 1024, AccessType.Public);

        var response = await sut.PrepareUploadAsync(request);

        Assert.False(string.IsNullOrWhiteSpace(response.UploadToken));
        Assert.Contains("png", response.AllowedExtensions);
    }

    [Fact]
    public async Task Upload_RejectsMissingToken()
    {
        var sut = Build();
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UploadAsync("", stream));
    }

    [Fact]
    public async Task Upload_RejectsExpiredToken()
    {
        var store = new FakeTokenStore { ExpireAll = true };
        var sut = Build(store);
        using var stream = new MemoryStream(new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A });

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UploadAsync("token", stream));
    }

    [Fact]
    public async Task Upload_RejectsWhenMagicNumberTypeIsNotAllowed()
    {
        _currentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());
        var store = new FakeTokenStore();
        var sut = Build(store, new UploadOptions
        {
            MaxSizeBytes = 1024 * 1024,
            AllowedExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "png" },
        });

        // Name says png, content is actually a PDF — must be rejected.
        await store.CreateAsync(new UploadTokenPayload("fake.png", 1024, AccessType.Public, null, null), 300, default);

        using var stream = new MemoryStream(new byte[] { 0x25, 0x50, 0x44, 0x46 });

        await Assert.ThrowsAsync<InvalidOperationException>(() => sut.UploadAsync("stored-token", stream));
    }

    [Fact]
    public async Task Upload_StoresBytesAndCreatesRecord()
    {
        _currentUser.Setup(x => x.UserId).Returns(Guid.NewGuid());
        _fileStorage
            .Setup(x => x.SaveAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("physicalname.png");
        _repository
            .Setup(x => x.IsShortCodeFreeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repository
            .Setup(x => x.IsFriendlyNameFreeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _repository
            .Setup(x => x.CreateAsync(It.IsAny<FileItem>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((FileItem f, CancellationToken _) => f);

        var store = new FakeTokenStore();
        var sut = Build(store);
        await store.CreateAsync(new UploadTokenPayload("photo.png", 8, AccessType.Public, null, null), 300, default);

        var png = new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A };
        using var stream = new MemoryStream(png);

        var response = await sut.UploadAsync("stored-token", stream);

        Assert.NotNull(response);
        _fileStorage.Verify(x => x.SaveAsync(response.FileId, "png", It.IsAny<Stream>(), It.IsAny<CancellationToken>()), Times.Once);
        _repository.Verify(x => x.CreateAsync(It.IsAny<FileItem>(), It.IsAny<CancellationToken>()), Times.Once);
        _metadataCache.Verify(x => x.WarmAsync(It.IsAny<FileItem>(), It.IsAny<CancellationToken>()), Times.Once);
        Assert.Equal("/i/" + response.ShortCode, response.Url);
    }
}

internal sealed class FakeTokenStore : IUploadTokenStore
{
    public bool ExpireAll { get; set; }
    private readonly Dictionary<string, UploadTokenPayload> _tokens = new();

    public Task<string> CreateAsync(UploadTokenPayload payload, int ttlSeconds, CancellationToken cancellationToken = default)
    {
        var token = "stored-token";
        _tokens[token] = payload;
        return Task.FromResult(token);
    }

    public Task<UploadTokenPayload?> ResolveAsync(string token, CancellationToken cancellationToken = default)
        => Task.FromResult(ExpireAll || !_tokens.TryGetValue(token, out var payload) ? null : payload);
}