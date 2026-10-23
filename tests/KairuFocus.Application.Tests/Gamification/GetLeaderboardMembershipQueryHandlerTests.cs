using KairuFocus.Application.Gamification.Queries.GetLeaderboardMembership;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Application.Tests.Gamification;

public sealed class GetLeaderboardMembershipQueryHandlerTests
{
    private static readonly DateTime JoinedAtUtc = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    private readonly FakeLeaderboardProfileRepository _repository = new();
    private readonly GetLeaderboardMembershipQueryHandler _sut;

    public GetLeaderboardMembershipQueryHandlerTests()
    {
        _sut = new GetLeaderboardMembershipQueryHandler(_repository, new FakeCurrentUserService());
    }

    [Fact]
    public async Task Should_ReturnPseudonym_When_UserIsParticipant()
    {
        _repository.Seed(FakeCurrentUserService.TestUserId, "LoutreTenace", JoinedAtUtc);
        _repository.Seed(UserId.New(), "HibouAttentif", JoinedAtUtc);

        var result = await _sut.Handle(new GetLeaderboardMembershipQuery());

        Assert.Equal(new GetLeaderboardMembershipResult(IsParticipant: true, Pseudonym: "LoutreTenace"), result);
    }

    [Fact]
    public async Task Should_ReturnNotParticipant_When_ProfileIsMissing()
    {
        _repository.Seed(UserId.New(), "HibouAttentif", JoinedAtUtc);

        var result = await _sut.Handle(new GetLeaderboardMembershipQuery());

        Assert.Equal(new GetLeaderboardMembershipResult(IsParticipant: false, Pseudonym: null), result);
    }
}
