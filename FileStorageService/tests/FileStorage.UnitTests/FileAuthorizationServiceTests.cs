using FileStorage.Application.Dtos;
using FileStorage.Application.Services;
using FileStorage.Domain.Enums;

namespace FileStorage.UnitTests;

public class FileAuthorizationServiceTests
{
    private readonly FileAuthorizationService _sut = new();
    private static readonly Guid UserId = Guid.NewGuid();

    private static CachedFileMeta Meta(AccessType accessType, Guid? owner = null, bool deleted = false)
        => new(
            Guid.NewGuid(),
            "code",
            "name",
            "Test title",
            "Test description",
            "file.pdf",
            "pdf",
            "application/pdf",
            100,
            "abc.pdf",
            accessType,
            owner,
            null,
            deleted,
            DateTime.UtcNow,
            "v1");

    [Theory]
    [InlineData(AccessType.Public, true)]
    [InlineData(AccessType.TokenProtected, false)]
    [InlineData(AccessType.GroupRestricted, false)]
    [InlineData(AccessType.Temporary, false)]
    [InlineData(AccessType.Static, false)]
    public void CanReadPublic_OnlyAllowsPublic(AccessType accessType, bool expected)
        => Assert.Equal(expected, _sut.CanReadPublic(Meta(accessType)));

    [Fact]
    public void CanReadPublic_False_WhenSoftDeleted()
        => Assert.False(_sut.CanReadPublic(Meta(AccessType.Public, deleted: true)));

    [Fact]
    public void CanRead_TokenProtected_AllowsOwnerAndAdmin()
    {
        var ownerMeta = Meta(AccessType.TokenProtected, UserId);
        Assert.True(_sut.CanRead(ownerMeta, UserId, isAdmin: false));
        Assert.True(_sut.CanRead(ownerMeta, null, isAdmin: true));
        Assert.False(_sut.CanRead(ownerMeta, Guid.NewGuid(), isAdmin: false));
        Assert.False(_sut.CanRead(ownerMeta, null, isAdmin: false));
    }

    [Fact]
    public void CanRead_GroupRestricted_OwnerAllowedOthersDeniedAtThisLayer()
    {
        var meta = Meta(AccessType.GroupRestricted, UserId);
        Assert.True(_sut.CanRead(meta, UserId, isAdmin: false));
        Assert.False(_sut.CanRead(meta, Guid.NewGuid(), isAdmin: false));
    }

    [Fact]
    public void CanRead_Temporary_NotReachableViaCodeEndpoints()
    {
        var meta = Meta(AccessType.Temporary, UserId);
        Assert.False(_sut.CanRead(meta, UserId, isAdmin: false));
        Assert.False(_sut.CanRead(meta, UserId, isAdmin: true));
    }

    [Fact]
    public void CanRead_False_WhenSoftDeleted()
    {
        var meta = Meta(AccessType.Public, deleted: true);
        Assert.False(_sut.CanRead(meta, null, isAdmin: false));
    }

    [Fact]
    public void CanManage_OnlyOwnerOrAdmin()
    {
        Assert.True(_sut.CanManage(UserId, UserId, isAdmin: false));
        Assert.True(_sut.CanManage(null, null, isAdmin: true));
        Assert.False(_sut.CanManage(UserId, Guid.NewGuid(), isAdmin: false));
        Assert.False(_sut.CanManage(null, null, isAdmin: false));
    }
}