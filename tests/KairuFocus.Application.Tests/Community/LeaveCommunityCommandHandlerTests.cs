using KairuFocus.Application.Community.Commands.LeaveCommunity;
using KairuFocus.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using CommunityErrors = KairuFocus.Domain.Community.DomainErrors;

namespace KairuFocus.Application.Tests.Community;

public sealed class LeaveCommunityCommandHandlerTests
{
    private readonly FakeMemberProfileRepository _profiles = new();
    private readonly UserId _me = UserId.New();
    private readonly LeaveCommunityCommandHandler _sut;

    public LeaveCommunityCommandHandlerTests()
    {
        _sut = new LeaveCommunityCommandHandler(
            _profiles,
            new MembershipCurrentUser(_me),
            NullLogger<LeaveCommunityCommandHandler>.Instance);
    }

    [Fact]
    public async Task Should_RemoveProfile_When_Member()
    {
        _profiles.Seed(_me, "bobby");
        _profiles.Seed(UserId.New(), "lea");

        var result = await _sut.Handle(new LeaveCommunityCommand());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Equal("lea", Assert.Single(_profiles.Stored).Name.Value);
        Assert.Equal(1, _profiles.TryRemoveCallCount);
    }

    [Fact]
    public async Task Should_ReturnNotMember_When_NoProfile()
    {
        _profiles.Seed(UserId.New(), "lea");

        var result = await _sut.Handle(new LeaveCommunityCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotMember, result.Error);
        Assert.Equal(0, _profiles.TryRemoveCallCount);
        Assert.Single(_profiles.Stored);
    }

    [Fact]
    public async Task Should_ReturnNotMember_When_RemovalRaced()
    {
        _profiles.Seed(_me, "bobby");
        _profiles.OnTryRemove = _ => _profiles.Delete(_me);

        var result = await _sut.Handle(new LeaveCommunityCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(CommunityErrors.Community.NotMember, result.Error);
        Assert.Equal(1, _profiles.TryRemoveCallCount);
    }
}
