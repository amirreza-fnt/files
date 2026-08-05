namespace FileStorage.Api.Helpers;

using FileStorage.Application.Dtos;
using Microsoft.Net.Http.Headers;

/// <summary>Builds streaming results with proper HTTP cache / range / disposition headers.</summary>
public static class FileStreamer
{
    public const string PublicShortCache = "public, max-age=3600";

    /// <summary>Streams a regular file with browser/SEO-friendly cache headers.</summary>
    public static IResult Stream(FileReadResult result, string cacheControl)
    {
        var inner = Results.File(
            result.OpenRead(),
            result.Metadata.MimeType,
            result.Metadata.OriginalFileName,
            enableRangeProcessing: true,
            lastModified: result.Metadata.CreatedAtUtc,
            entityTag: new EntityTagHeaderValue($"\"{result.VersionToken}\""));

        return new HeaderResult(inner, cacheControl);
    }

    /// <summary>Streams a static file with strong cache headers (immutable by design).</summary>
    public static IResult StreamStatic(StaticFileReadResult result)
    {
        var inner = Results.File(
            result.OpenRead(),
            result.MimeType,
            result.OriginalFileName,
            enableRangeProcessing: true,
            entityTag: new EntityTagHeaderValue($"\"{result.VersionToken}\""));

        return new HeaderResult(inner, $"public, max-age={result.CacheControlSeconds}, immutable");
    }
}

/// <summary>Wraps a result and appends a Cache-Control header.</summary>
internal sealed class HeaderResult : IResult
{
    private readonly IResult _inner;
    private readonly string _cacheControl;

    public HeaderResult(IResult inner, string cacheControl)
    {
        _inner = inner;
        _cacheControl = cacheControl;
    }

    public async Task ExecuteAsync(HttpContext httpContext)
    {
        httpContext.Response.Headers.CacheControl = _cacheControl;
        await _inner.ExecuteAsync(httpContext);
    }
}