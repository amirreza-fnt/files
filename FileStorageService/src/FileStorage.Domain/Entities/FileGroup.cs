namespace FileStorage.Domain.Entities;

/// <summary>
/// Grouping module that already exists in the wider platform.
/// We integrate with it (FK reference) instead of rebuilding it.
/// </summary>
public class FileGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public Guid? OwnerUserId { get; set; }
    public DateTime CreatedAtUtc { get; set; }

    public ICollection<FileGroupMember> Members { get; set; } = new List<FileGroupMember>();
}