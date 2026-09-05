namespace FileStorage.Application.Interfaces;

using FileStorage.Application.Dtos;
using FileStorage.Domain.Enums;

/// <summary>Payload carried by a prepare-upload token (Redis, short TTL).</summary>
public sealed record UploadTokenPayload(
    string FileName,
    string Title,
    string? Description,
    long SizeBytes,
    AccessType AccessType,
    Guid? GroupId,
    Guid? OwnerUserId);

/// <summary>Short-lived upload token store (Redis <c>upload:token:*</c>).</summary>
public interface IUploadTokenStore
{
    Task<string> CreateAsync(UploadTokenPayload payload, int ttlSeconds, CancellationToken cancellationToken = default);

    Task<UploadTokenPayload?> ResolveAsync(string token, CancellationToken cancellationToken = default);
}