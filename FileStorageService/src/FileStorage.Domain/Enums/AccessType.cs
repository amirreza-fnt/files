namespace FileStorage.Domain.Enums;

/// <summary>
/// Controls how a stored file can be accessed.
/// This aligns with the public/token/group/temporary/static modes.
/// </summary>
public enum AccessType
{
    /// <summary>Anyone can read via public short link <c>/i/CODE</c> or friendly name <c>/f/NAME</c>.</summary>
    Public = 1,

    /// <summary>Requires a valid JWT that entitles the calling principal to the file.</summary>
    TokenProtected = 2,

    /// <summary>Only members of the linked group can read (checked via <c>/g/CODE</c>).</summary>
    GroupRestricted = 3,

    /// <summary>Servable only through a Redis-backed temporary link <c>/t/TOKEN</c>.</summary>
    Temporary = 4,

    /// <summary>Special static file served with strong HTTP caching (<c>/c/CODE</c>).</summary>
    Static = 5
}