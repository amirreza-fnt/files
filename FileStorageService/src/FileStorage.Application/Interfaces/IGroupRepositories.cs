namespace FileStorage.Application.Interfaces;

using FileStorage.Domain.Entities;

public interface IStaticFileRepository
{
    Task<StaticFile?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default);
}

public interface IFileGroupRepository
{
    Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default);

    Task<FileGroup?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken = default);
}