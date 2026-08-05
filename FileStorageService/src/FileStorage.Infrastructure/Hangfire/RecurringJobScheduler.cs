namespace FileStorage.Infrastructure.Hangfire;

using FileStorage.Application.Options;
using Hangfire;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

/// <summary>
/// Schedules the nightly purge job WITHOUT blocking application startup.
/// MSSQL is on a separate server; if it is briefly unreachable at boot the
/// app still starts and the job is scheduled on the next retry.
/// </summary>
public sealed class RecurringJobScheduler : BackgroundService
{
    private const string JobId = "purge-deleted-files";

    private readonly IConfiguration _configuration;
    private readonly ILogger<RecurringJobScheduler> _logger;
    private readonly TimeSpan _retryInterval = TimeSpan.FromSeconds(60);

    public RecurringJobScheduler(IConfiguration configuration, ILogger<RecurringJobScheduler> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var cron = _configuration.GetSection(DeleteOptions.SectionName).Get<DeleteOptions>()?.PurgeCron ?? "0 3 * * *";

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                global::Hangfire.RecurringJob.AddOrUpdate<PurgeDeletedFilesJob>(
                    JobId,
                    job => job.RunAsync(CancellationToken.None),
                    cron);

                _logger.LogInformation("Nightly purge recurring job ({JobId}) scheduled with cron '{Cron}'.", JobId, cron);
                return;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Could not schedule recurring job — storage unavailable. Retrying in {Seconds}s.",
                    _retryInterval.TotalSeconds);
                await Task.Delay(_retryInterval, stoppingToken);
            }
        }
    }
}