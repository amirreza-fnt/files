namespace FileStorage.Api.Controllers;

using FileStorage.Api.Helpers;
using FileStorage.Application.Interfaces;
using FileStorage.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

/// <summary>
/// Public read endpoints. Metadata reads go through the Redis Cache-Aside layer
/// (never a direct MSSQL round-trip for hot paths).
/// </summary>
[ApiController]
[Route("")]
[AllowAnonymous]
public sealed class PublicFilesController : ControllerBase
{
    private readonly IFileReadService _readService;
    private readonly ICurrentUserAccessor _currentUser;

    public PublicFilesController(IFileReadService readService, ICurrentUserAccessor currentUser)
    {
        _readService = readService;
        _currentUser = currentUser;
    }

    /// <summary>GET /f/{friendlyName} — public file by friendly name.</summary>
    [HttpGet("f/{friendlyName}")]
    public async Task<IResult> ByFriendlyName(string friendlyName, CancellationToken cancellationToken)
    {
        var result = await _readService.ReadByFriendlyNameAsync(friendlyName, cancellationToken);
        return result is null
            ? Results.NotFound()
            : FileStreamer.Stream(result, FileStreamer.PublicShortCache);
    }

    /// <summary>GET /i/{shortCode} — short link (public or token-protected).</summary>
    [HttpGet("i/{shortCode}")]
    public async Task<IResult> ByShortCode(string shortCode, CancellationToken cancellationToken)
    {
        var result = await _readService.ReadByShortCodeAsync(shortCode, cancellationToken);
        return result is null
            ? Results.NotFound()
            : FileStreamer.Stream(result, FileStreamer.PublicShortCache);
    }

    /// <summary>GET /c/{shortCode} — static file with strong HTTP caching (Nginx cache layer).</summary>
    [HttpGet("c/{shortCode}")]
    public async Task<IResult> ByStaticCode(string shortCode, CancellationToken cancellationToken)
    {
        var result = await _readService.ReadStaticAsync(shortCode, cancellationToken);
        return result is null
            ? Results.NotFound()
            : FileStreamer.StreamStatic(result);
    }

    /// <summary>GET /g/{shortCode} — group-restricted file (membership checked + cached).</summary>
    [HttpGet("g/{shortCode}")]
    public async Task<IResult> ByGroup(string shortCode, CancellationToken cancellationToken)
    {
        if (!_currentUser.UserId.HasValue)
        {
            return Results.Challenge();
        }

        var result = await _readService.ReadByGroupAsync(shortCode, _currentUser.UserId.Value, cancellationToken);
        return result is null
            ? Results.NotFound()
            : FileStreamer.Stream(result, FileStreamer.PublicShortCache);
    }

    /// <summary>GET /t/{token} — temporary link, resolved purely from Redis TTL. No DB query.</summary>
    [HttpGet("t/{token}")]
    public async Task<IResult> ByTempToken(string token, CancellationToken cancellationToken)
    {
        var result = await _readService.ReadByTempTokenAsync(token, cancellationToken);
        return result is null
            ? Results.NotFound("Link is missing or expired.")
            : FileStreamer.Stream(result, "private, max-age=0");
    }
}
