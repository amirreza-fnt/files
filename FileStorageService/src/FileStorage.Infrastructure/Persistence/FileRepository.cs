namespace FileStorage.Infrastructure.Persistence;

using FileStorage.Application.Interfaces;
using FileStorage.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class FileRepository : IFileRepository
{
    private readonly FileStorageDbContext _context;

    public FileRepository(FileStorageDbContext context)
    {
        _context = context;
    }

    public async Task<FileItem?> GetByShortCodeAsync(string shortCode, CancellationToken cancellationToken = default)
        => await _context.Files.SingleOrDefaultAsync(f => f.ShortCode == shortCode, cancellationToken);

    public async Task<FileItem?> GetByFriendlyNameAsync(string friendlyName, CancellationToken cancellationToken = default)
        => await _context.Files.SingleOrDefaultAsync(f => f.FriendlyName == friendlyName, cancellationToken);

    public async Task<FileItem?> GetByIdAsync(Guid fileId, CancellationToken cancellationToken = default)
        => await _context.Files.SingleOrDefaultAsync(f => f.Id == fileId, cancellationToken);

    public async Task<FileItem?> GetByIdIncludingDeletedAsync(Guid fileId, CancellationToken cancellationToken = default)
        => await _context.Files.IgnoreQueryFilters().SingleOrDefaultAsync(f => f.Id == fileId, cancellationToken);

    public async Task<bool> IsShortCodeFreeAsync(string shortCode, CancellationToken cancellationToken = default)
        => !await _context.Files.IgnoreQueryFilters().AnyAsync(f => f.ShortCode == shortCode, cancellationToken);

    public async Task<bool> IsFriendlyNameFreeAsync(string friendlyName, CancellationToken cancellationToken = default)
        => !await _context.Files.IgnoreQueryFilters().AnyAsync(f => f.FriendlyName == friendlyName, cancellationToken);

    public async Task<FileItem> CreateAsync(FileItem file, CancellationToken cancellationToken = default)
    {
        _context.Files.Add(file);
        await _context.SaveChangesAsync(cancellationToken);
        return file;
    }

    public async Task UpdateAsync(FileItem file, CancellationToken cancellationToken = default)
    {
        _context.Files.Update(file);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<FileItem>> GetDeletedOlderThanAsync(DateTime thresholdUtc, int take, CancellationToken cancellationToken = default)
        => await _context.Files
            .IgnoreQueryFilters()
            .Where(f => f.IsDeleted && f.DeletedAtUtc != null && f.DeletedAtUtc < thresholdUtc)
            .OrderBy(f => f.DeletedAtUtc)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task HardDeleteAsync(Guid fileId, CancellationToken cancellationToken = default)
    {
        await _context.Files
            .IgnoreQueryFilters()   // soft-deleted rows must also be reachable for hard delete
            .Where(f => f.Id == fileId)
            .ExecuteDeleteAsync(cancellationToken);
    }
}