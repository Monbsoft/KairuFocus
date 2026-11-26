using KairuFocus.Application.Community.Commands.ConsentToPresence;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Identity;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Tests.Community;

public sealed class ConsentToPresenceCommandHandlerTests
{
    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly FakeMemberPresenceRepository _presences = new();
    private readonly UserId _me = UserId.New();
    private readonly ConsentToPresenceCommandHandler _sut;

    public ConsentToPresenceCommandHandlerTests() =>
        _sut = new ConsentToPresenceCommandHandler(
            _profiles,
            _presences,
            new MembershipCurrentUser(_me),
            new FixedTimeProvider(new DateTimeOffset(MembershipTestData.NowUtc)));

    [Fact]
    public async Task Should_StoreConsentAndRecordSeen_When_Member()
    {
        _profiles.Seed(_me, "bobby", presenceConsented: false);

        var result = await _sut.Handle(new ConsentToPresenceCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(MembershipTestData.NowUtc, Assert.Single(_profiles.Stored).PresenceConsentedAtUtc);
        Assert.Equal((_me, MembershipTestData.NowUtc), Assert.Single(_presences.Recorded));
    }

    [Fact]
    public async Task Should_FailNotMember_When_ConsentingWithoutProfile()
    {
        var result = await _sut.Handle(new ConsentToPresenceCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotMember, result.Error);
        Assert.Empty(_presences.Recorded);
    }
}
