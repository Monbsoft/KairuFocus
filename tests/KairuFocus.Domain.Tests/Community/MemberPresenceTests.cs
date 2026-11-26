using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Tests.Community;

public sealed class MemberPresenceTests
{
    private static readonly UserId User = UserId.From(new Guid("00000000-0000-0000-0000-000000000001"));
    private static readonly DateTime SeenAtUtc = new(2026, 11, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Should_RecordFirstSighting_When_Created()
    {
        var presence = MemberPresence.Record(User, SeenAtUtc);

        Assert.Equal(User, presence.Id);
        Assert.Equal(SeenAtUtc, presence.LastSeenAtUtc);
    }

    [Fact]
    public void Should_MoveForward_When_SeenLater()
    {
        var presence = MemberPresence.Record(User, SeenAtUtc);

        presence.SeenAt(SeenAtUtc.AddMinutes(1));

        Assert.Equal(SeenAtUtc.AddMinutes(1), presence.LastSeenAtUtc);
    }

    [Fact]
    public void Should_KeepLatest_When_SeenEarlier()
    {
        var presence = MemberPresence.Record(User, SeenAtUtc);

        presence.SeenAt(SeenAtUtc.AddMinutes(-1));

        Assert.Equal(SeenAtUtc, presence.LastSeenAtUtc);
    }
}
