using KairuFocus.Application.Gamification.Commands.LeaveLeaderboard;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Gamification;
using Microsoft.Extensions.Logging.Abstractions;
using GamificationErrors = KairuFocus.Domain.Gamification.DomainErrors;

namespace KairuFocus.Application.Tests.Gamification;

public sealed class LeaveLeaderboardCommandHandlerTests
{
    private static readonly DateTime JoinedAtUtc = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);

    private readonly FakeLeaderboardProfileRepository _repository = new();
    private readonly LeaveLeaderboardCommandHandler _sut;

    public LeaveLeaderboardCommandHandlerTests()
    {
        _sut = new LeaveLeaderboardCommandHandler(
            _repository,
            new FakeCurrentUserService(),
            NullLogger<LeaveLeaderboardCommandHandler>.Instance);
    }

    [Fact]
    public async Task Should_RemoveProfile_When_UserIsParticipant()
    {
        _repository.Seed(FakeCurrentUserService.TestUserId, "LoutreTenace", JoinedAtUtc);

        var result = await _sut.Handle(new LeaveLeaderboardCommand());

        Assert.True(result.IsSuccess);
        Assert.Null(result.Error);
        Assert.Empty(_repository.Stored);
        Assert.Equal(1, _repository.TryRemoveCallCount);
    }

    [Fact]
    public void Should_KeepXpGains_When_UserLeaves()
    {
        // The ledger is out of reach by construction: the handler does not depend on IXpGainRepository.
        var dependencies = typeof(LeaveLeaderboardCommandHandler)
            .GetConstructors()
            .Single()
            .GetParameters()
            .Select(p => p.ParameterType);

        Assert.DoesNotContain(typeof(IXpGainRepository), dependencies);
    }

    [Fact]
    public async Task Should_ReturnNotParticipating_When_ProfileIsMissing()
    {
        var result = await _sut.Handle(new LeaveLeaderboardCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(GamificationErrors.Gamification.NotParticipating, result.Error);
        Assert.Equal(0, _repository.TryRemoveCallCount);
    }

    [Fact]
    public async Task Should_ReturnNotParticipating_When_ProfileWasRemovedConcurrently()
    {
        // Two simultaneous leaves (two tabs): both read the profile, the other one deletes it first.
        _repository.Seed(FakeCurrentUserService.TestUserId, "LoutreTenace", JoinedAtUtc);
        _repository.OnTryRemove = profile => _repository.Delete(profile.Id);

        var result = await _sut.Handle(new LeaveLeaderboardCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(GamificationErrors.Gamification.NotParticipating, result.Error);
        Assert.Equal(1, _repository.TryRemoveCallCount);
        Assert.Empty(_repository.Stored);
    }
}
