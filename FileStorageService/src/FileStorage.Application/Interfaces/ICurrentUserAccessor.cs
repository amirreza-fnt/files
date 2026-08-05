namespace FileStorage.Application.Interfaces;

/// <summary>Resolves the current request's principal (UserId / roles / anonymity).</summary>
public interface ICurrentUserAccessor
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    bool IsAdmin { get; }
}