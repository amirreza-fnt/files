namespace FileStorage.Application.Interfaces;

/// <summary>Temp-link store backed by Redis TTL (<c>temp:link:*</c> prefix).</summary>
public interface ITempLinkStore
{
    /// <summary>Stores a token -> fileId mapping that expires after the given seconds.</summary>
    Task<string> CreateAsync(Guid fileId, int ttlSeconds, CancellationToken cancellationToken = default);

    /// <summary>Returns the fileId if the token is still valid, else null.</summary>
    Task<Guid?> ResolveAsync(string token, CancellationToken cancellationToken = default);
}