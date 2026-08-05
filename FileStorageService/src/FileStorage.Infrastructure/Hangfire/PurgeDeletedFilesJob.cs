namespace FileStorage.Infrastructure.Hangfire;

using FileStorage.Application.Services;
using Microsoft.Extensions.Logging;

/// <summary>
/// Nightly recurring job: purges soft-deleted files older than the grace period.
/// Registered via Hangfire CRON (default 03:00 UTC). Coordinates with Redis by
/// re-invalidating cache (idempotent) after each purge.
/// </summary>
public sealed class PurgeDeletedFilesJob
{
    private readonly IFileDeleteService _deleteService;
    private readonly ILogger<PurgeDeletedFilesJob> _logger;

    public PurgeDeletedFilesJob(IFileDeleteService deleteService, ILogger<PurgeDeletedFilesJob> logger)
    {
        _deleteService = deleteService;
        _logger = logger;
    }

    public async Task RunAsync(CancellationToken cancellationToken = default)
    {
        var purged = await _deleteService.PurgeExpiredAsync(cancellationToken);
        _logger.LogInformation("Nightly purge finished. Purged {Count} file(s).", purged);
    }
}