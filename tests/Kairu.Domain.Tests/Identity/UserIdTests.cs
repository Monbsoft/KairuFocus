using Kairu.Domain.Identity;

namespace Kairu.Domain.Tests.Identity;

public sealed class UserIdTests
{
    [Fact]
    public void Should_CreateUserId_When_ValidGuidProvided()
    {
        var guid = Guid.NewGuid();
        var id = UserId.From(guid);

        Assert.Equal(guid, id.Value);
    }

    [Fact]
    public void Should_BeEqual_When_SameGuid()
    {
        var guid = Guid.NewGuid();
        var id1 = UserId.From(guid);
        var id2 = UserId.From(guid);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Should_NotBeEqual_When_DifferentGuids()
    {
        var id1 = UserId.New();
        var id2 = UserId.New();

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void Should_ReturnGuidString_When_ToStringCalled()
    {
        var guid = Guid.NewGuid();
        var id = UserId.From(guid);

        Assert.Equal(guid.ToString(), id.ToString());
    }

    [Fact]
    public void Should_GenerateUniqueIds_When_NewCalledMultipleTimes()
    {
        var id1 = UserId.New();
        var id2 = UserId.New();

        Assert.NotEqual(id1, id2);
    }
}
