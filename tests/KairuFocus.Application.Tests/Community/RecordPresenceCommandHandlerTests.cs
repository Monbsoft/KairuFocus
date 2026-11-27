using KairuFocus.Application.Community.Commands.RecordPresence;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Application.Tests.Community;

public sealed class RecordPresenceCommandHandlerTests
{
    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakeMemberPresenceRepository _presences = new();
    private readonly UserId _me = UserId.New();
    private readonly RecordPresenceCommandHandler _sut;

    public RecordPresenceCommandHandlerTests() =>
        _sut = new RecordPresenceCommandHandler(
            _profiles,
            _presences,
            new MembershipCurrentUser(_me),
            new FixedTimeProvider(new DateTimeOffset(MembershipTestData.NowUtc)));

    [Fact]
    public async Task Should_RecordSeenNow_When_MemberConsented()
    {
        _profiles.Seed(_me, "bobby");

        var result = await _sut.Handle(new RecordPresenceCommand());

        Assert.True(result.Recorded);
        Assert.Equal((_me, MembershipTestData.NowUtc), Assert.Single(_presences.Recorded));
    }

    [Fact]
    public async Task Should_StoreNothing_When_NotMember()
    {
        var result = await _sut.Handle(new RecordPresenceCommand());

        Assert.False(result.Recorded);
        Assert.Empty(_presences.Recorded);
    }

    [Fact]
    public async Task Should_StoreNothing_When_MemberHasNotConsented()
    {
        _profiles.Seed(_me, "bobby", presenceConsented: false);

        var result = await _sut.Handle(new RecordPresenceCommand());

        Assert.False(result.Recorded);
        Assert.Empty(_presences.Recorded);
    }

    [Fact]
    public async Task Should_NotUpdateProfile_When_RecordingPresence()
    {
        _profiles.Seed(_me, "bobby");

        await _sut.Handle(new RecordPresenceCommand());

        Assert.Equal(0, _profiles.TryUpdateCallCount);
    }
}
