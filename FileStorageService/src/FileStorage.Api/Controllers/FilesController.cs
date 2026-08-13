namespace FileStorage.Api.Controllers;

using FileStorage.Application.Dtos;
using FileStorage.Application.Interfaces;
using FileStorage.Application.Options;
using FileStorage.Application.Services;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

/// <summary>Authenticated management endpoints: two-step upload, delete, info, temp links.</summary>
[ApiController]
[Route("api/files")]
public sealed class FilesController : ControllerBase
{
    private readonly IFileUploadService _uploadService;
    private readonly IFileDeleteService _deleteService;
    private readonly IFileReadService _readService;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITempLinkStore _tempLinkStore;
    private readonly IValidator<PrepareUploadRequest> _prepareValidator;
    private readonly IOptions<InternalAuthOptions> _internalAuth;

    public FilesController(
        IFileUploadService uploadService,
        IFileDeleteService deleteService,
        IFileReadService readService,
        ICurrentUserAccessor currentUser,
        ITempLinkStore tempLinkStore,
        IValidator<PrepareUploadRequest> prepareValidator,
        IOptions<InternalAuthOptions> internalAuth)
    {
        _uploadService = uploadService;
        _deleteService = deleteService;
        _readService = readService;
        _currentUser = currentUser;
        _tempLinkStore = tempLinkStore;
        _prepareValidator = prepareValidator;
        _internalAuth = internalAuth;
    }

    /// <summary>POST /api/files/prepare-upload — step 1: request an upload token.</summary>
    [HttpPost("prepare-upload")]
    public async Task<ActionResult<PrepareUploadResponse>> PrepareUpload(PrepareUploadRequest request, CancellationToken cancellationToken)
    {
        var validation = await _prepareValidator.ValidateAsync(request, cancellationToken);
        if (!validation.IsValid)
        {
            return BadRequest(validation.ToDictionary());
        }

        var response = await _uploadService.PrepareUploadAsync(request, cancellationToken);
        return Ok(response);
    }

    /// <summary>POST /api/files/upload — step 2: upload real bytes with the token.</summary>
    [HttpPost("upload")]
    public async Task<ActionResult<UploadResponse>> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        if (file is null)
        {
            return BadRequest("A file part is required.");
        }

        // Read header manually (kept out of the action signature so Swashbuckle
        // can generate the multipart schema correctly).
        if (!Request.Headers.TryGetValue("UploadToken", out var tokenValues) || string.IsNullOrWhiteSpace(tokenValues.ToString()))
        {
            return BadRequest("UploadToken header is required.");
        }

        await using var stream = file.OpenReadStream();
        var response = await _uploadService.UploadAsync(tokenValues.ToString()!, stream, cancellationToken);
        return Ok(response);
    }

    /// <summary>DELETE /api/files/{id} — soft delete + immediate Redis invalidation.</summary>
    [HttpDelete("{id:guid}")]
    [Authorize]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        var deleted = await _deleteService.SoftDeleteAsync(id, cancellationToken);
        return deleted ? NoContent() : Forbid();
    }

    /// <summary>
    /// GET /api/files/{id} — file metadata.
    /// Auth: JWT (owner or Admin) OR valid X-Api-Key for sibling microservices.
    /// </summary>
    [HttpGet("{id:guid}")]
    [AllowAnonymous]
    public async Task<ActionResult<FileInfoResponse>> GetInfo(Guid id, CancellationToken cancellationToken)
    {
        var serviceCaller = IsValidServiceKey();
        var jwtAuthenticated = User.Identity?.IsAuthenticated == true;

        if (!serviceCaller && !jwtAuthenticated)
        {
            return Unauthorized();
        }

        var info = await _readService.GetInfoAsync(id, cancellationToken);
        if (info is null)
        {
            return NotFound();
        }

        if (!serviceCaller && !_currentUser.IsAdmin && info.OwnerUserId != _currentUser.UserId)
        {
            return Forbid();
        }

        return Ok(info);
    }

    private bool IsValidServiceKey()
    {
        var headerName = string.IsNullOrWhiteSpace(_internalAuth.Value.HeaderName)
            ? "X-Api-Key"
            : _internalAuth.Value.HeaderName;

        if (!Request.Headers.TryGetValue(headerName, out var values))
        {
            return false;
        }

        var key = values.ToString()?.Trim();
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return _internalAuth.Value.ApiKeys.Any(k =>
            !string.IsNullOrEmpty(k.Key)
            && k.Key.Equals(key, StringComparison.Ordinal));
    }

    /// <summary>POST /api/files/{id}/temp-link — create a Redis TTL backed temporary link.</summary>
    [HttpPost("{id:guid}/temp-link")]
    [Authorize]
    public async Task<ActionResult<TempLinkResponse>> CreateTempLink(Guid id, [FromBody] TempLinkRequest request, CancellationToken cancellationToken)
    {
        var info = await _readService.GetInfoAsync(id, cancellationToken);
        if (info is null)
        {
            return NotFound();
        }

        if (!_currentUser.IsAdmin && info.OwnerUserId != _currentUser.UserId)
        {
            return Forbid();
        }

        var token = await _tempLinkStore.CreateAsync(id, request.TtlSeconds, cancellationToken);
        return Ok(new TempLinkResponse($"/t/{token}", request.TtlSeconds));
    }
}

public sealed record TempLinkRequest(int TtlSeconds);

public sealed record TempLinkResponse(string Url, int TtlSeconds);