namespace FileStorage.Domain.Entities;

/// <summary>
/// Membership row linking a user to a group. Existence is cached in Redis
/// with a short TTL because access rights can change.
/// </summary>
public class FileGroupMember
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public Guid UserId { get; set; }
    public DateTime JoinedAtUtc { get; set; }

    public FileGroup Group { get; set; } = null!;
}