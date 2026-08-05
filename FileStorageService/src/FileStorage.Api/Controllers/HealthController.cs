namespace FileStorage.Api.Controllers;

using FileStorage.Application.Interfaces;
using FileStorage.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

/// <summary>Readiness/liveness probe for systemd and Nginx.</summary>
[ApiController]
[Route("api/health")]
[AllowAnonymous]
public sealed class HealthController : ControllerBase
{
    private readonly FileStorageDbContext _db;
    private readonly IConnectionMultiplexer _redis;

    public HealthController(FileStorageDbContext db, IConnectionMultiplexer redis)
    {
        _db = db;
        _redis = redis;
    }

    [HttpGet]
    public async Task<IActionResult> Check(CancellationToken cancellationToken)
    {
        // Short probe so Nginx/systemd health checks stay snappy even when the
        // remote MSSQL is down (retry policy is bypassed via timeout).
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(4));

        try
        {
            await _db.Database.ExecuteSqlRawAsync("SELECT 1", cts.Token);
        }
        catch
        {
            return StatusCode(503, new { status = "degraded", database = "unreachable", redis = _redis.IsConnected });
        }

        return Ok(new
        {
            status = "healthy",
            database = "ok",
            redis = _redis.IsConnected,
        });
    }
}