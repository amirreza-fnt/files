namespace FileStorage.Infrastructure.Persistence;

using FileStorage.Application.Cache;
using FileStorage.Application.Interfaces;
using FileStorage.Application.Options;
using FileStorage.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

/// <summary>
/// Group repository. Membership checks are cached in Redis with a SHORT TTL
/// (access rights can change) to reduce repeated MSSQL round-trips.
/// </summary>
public sealed class FileGroupRepository : IFileGroupRepository
{
    private const string BoolTrue = "1";
    private const string BoolFalse = "0";

    private readonly FileStorageDbContext _context;
    private readonly ICacheService _cache;
    private readonly CacheOptions _options;

    public FileGroupRepository(FileStorageDbContext context, ICacheService cache, IOptions<CacheOptions> options)
    {
        _context = context;
        _cache = cache;
        _options = options.Value;
    }

    public async Task<bool> IsMemberAsync(Guid groupId, Guid userId, CancellationToken cancellationToken = default)
    {
        var key = CacheKeys.GroupMembership(groupId, userId);
        var cached = await _cache.GetStringAsync(key, cancellationToken);
        if (cached is not null)
        {
            return cached == BoolTrue;
        }

        var isMember = await _context.FileGroupMembers.AnyAsync(m => m.GroupId == groupId && m.UserId == userId, cancellationToken);

        await _cache.SetStringAsync(key, isMember ? BoolTrue : BoolFalse, _options.GroupMembershipTtl, cancellationToken);
        return isMember;
    }

    public async Task<FileGroup?> GetByIdAsync(Guid groupId, CancellationToken cancellationToken = default)
        => await _context.FileGroups.SingleOrDefaultAsync(g => g.Id == groupId, cancellationToken);
}