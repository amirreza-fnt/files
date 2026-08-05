namespace FileStorage.Application.Options;

/// <summary>Soft-delete / nightly purge settings.</summary>
public sealed class DeleteOptions
{
    public const string SectionName = "Delete";

    /// <summary>Hours a soft-deleted file is kept before the nightly job removes it.</summary>
    public int GracePeriodHours { get; set; } = 24;

    /// <summary>Time (UTC) the recurring purge job runs.</summary>
    public string PurgeCron { get; set; } = "0 3 * * *"; // every day at 03:00 UTC

    /// <summary>If true the file bytes are deleted immediately on request (no undo), else left for nightly job.</summary>
    public bool DeleteBytesImmediately { get; set; } = false;
}