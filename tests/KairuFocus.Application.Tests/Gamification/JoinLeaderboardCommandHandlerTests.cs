using KairuFocus.Application.Gamification.Commands.JoinLeaderboard;
using KairuFocus.Application.Gamification.Commands.LeaveLeaderboard;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using GamificationErrors = KairuFocus.Domain.Gamification.DomainErrors;

namespace KairuFocus.Application.Tests.Gamification;

public sealed class JoinLeaderboardCommandHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 23, 9, 30, 0, TimeSpan.Zero);
    private static readonly DateTime EarlierUtc = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly UserId Me = FakeCurrentUserService.TestUserId;

    private readonly FakeLeaderboardProfileRepository _repository = new();
    private readonly JoinLeaderboardCommandHandler _sut;

    public JoinLeaderboardCommandHandlerTests()
    {
        _sut = new JoinLeaderboardCommandHandler(
            _repository,
            new FakeCurrentUserService(),
            new FixedTimeProvider(Now),
            NullLogger<JoinLeaderboardCommandHandler>.Instance);
    }

    [Fact]
    public async Task Should_CreateProfileWithGeneratedPseudonym_When_UserIsNotParticipant()
    {
        var result = await _sut.Handle(new JoinLeaderboardCommand());

        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Pseudonym);
        Assert.True(Pseudonym.Create(result.Pseudonym).IsSuccess);
        var stored = Assert.Single(_repository.Stored);
        Assert.Equal(Me, stored.UserId);
        Assert.Equal(result.Pseudonym, stored.Pseudonym.Value);
        Assert.Equal(_repository.CheckedCandidates[^1], stored.Pseudonym);
    }

    [Fact]
    public async Task Should_DateProfileWithTimeProvider_When_UserJoins()
    {
        await _sut.Handle(new JoinLeaderboardCommand());

        var stored = Assert.Single(_repository.Stored);
        Assert.Equal(Now.UtcDateTime, stored.JoinedAtUtc);
    }

    [Fact]
    public async Task Should_ReturnAlreadyParticipating_When_ProfileExists()
    {
        _repository.Seed(Me, "LoutreTenace", EarlierUtc);

        var result = await _sut.Handle(new JoinLeaderboardCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(GamificationErrors.Gamification.AlreadyParticipating, result.Error);
        Assert.Null(result.Pseudonym);
        Assert.Empty(_repository.CheckedCandidates);
        Assert.Equal(0, _repository.TryAddCallCount);
    }

    [Fact]
    public async Task Should_ReturnPoolExhausted_When_NoPseudonymIsAvailable()
    {
        _repository.TakenCandidateCount = 15;

        var result = await _sut.Handle(new JoinLeaderboardCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(GamificationErrors.Gamification.PseudonymPoolExhausted, result.Error);
        Assert.Equal(0, _repository.TryAddCallCount);
        Assert.Empty(_repository.Stored);
    }

    [Fact]
    public async Task Should_ReturnAlreadyParticipating_When_ConcurrentJoinCreatedProfile()
    {
        // Double submission: the other request creates the profile between the check and the insert.
        _repository.OnTryAdd = _ => _repository.Seed(Me, "HibouAttentif", Now.UtcDateTime);

        var result = await _sut.Handle(new JoinLeaderboardCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(GamificationErrors.Gamification.AlreadyParticipating, result.Error);
        var stored = Assert.Single(_repository.Stored);
        Assert.Equal("HibouAttentif", stored.Pseudonym.Value);
    }

    [Fact]
    public async Task Should_ReturnPoolExhausted_When_PseudonymWasTakenConcurrently()
    {
        // Someone else takes the drawn pseudonym between the check and the insert.
        var someoneElse = UserId.New();
        _repository.OnTryAdd = profile => _repository.Seed(someoneElse, profile.Pseudonym.Value, Now.UtcDateTime);

        var result = await _sut.Handle(new JoinLeaderboardCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(GamificationErrors.Gamification.PseudonymPoolExhausted, result.Error);
        var stored = Assert.Single(_repository.Stored);
        Assert.Equal(someoneElse, stored.UserId);
    }

    [Fact]
    public async Task Should_GrantNewPseudonym_When_UserRejoinsAfterLeaving()
    {
        _repository.Seed(Me, "LoutreTenace", EarlierUtc);
        var leave = new LeaveLeaderboardCommandHandler(
            _repository, new FakeCurrentUserService(), NullLogger<LeaveLeaderboardCommandHandler>.Instance);
        await leave.Handle(new LeaveLeaderboardCommand());

        var result = await _sut.Handle(new JoinLeaderboardCommand());

        // A brand-new profile: freshly drawn pseudonym, new join date; the old one is not remembered.
        Assert.True(result.IsSuccess);
        var stored = Assert.Single(_repository.Stored);
        Assert.Equal(Me, stored.UserId);
        Assert.Equal(_repository.CheckedCandidates[^1], stored.Pseudonym);
        Assert.Equal(result.Pseudonym, stored.Pseudonym.Value);
        Assert.Equal(Now.UtcDateTime, stored.JoinedAtUtc);
    }
}
