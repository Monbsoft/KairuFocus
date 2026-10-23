using KairuFocus.Application.Gamification.Commands.RerollPseudonym;
using KairuFocus.Application.Tests.Common;
using KairuFocus.Domain.Gamification;
using KairuFocus.Domain.Identity;
using Microsoft.Extensions.Logging.Abstractions;
using GamificationErrors = KairuFocus.Domain.Gamification.DomainErrors;

namespace KairuFocus.Application.Tests.Gamification;

public sealed class RerollPseudonymCommandHandlerTests
{
    private const string CurrentPseudonym = "LoutreTenace";
    private const int Seed = 42;
    private static readonly DateTime JoinedAtUtc = new(2026, 9, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly UserId Me = FakeCurrentUserService.TestUserId;

    private readonly FakeLeaderboardProfileRepository _repository = new();
    private readonly RerollPseudonymCommandHandler _sut;

    public RerollPseudonymCommandHandlerTests()
    {
        _sut = new RerollPseudonymCommandHandler(
            _repository,
            new FakeCurrentUserService(),
            NullLogger<RerollPseudonymCommandHandler>.Instance);
    }

    private FakeLeaderboardProfileRepository.StoredProfile MyProfile => _repository.Stored.Single(p => p.UserId == Me);

    /// <summary>Handler whose first draw is <see cref="FirstSeededDraw"/>.</summary>
    private RerollPseudonymCommandHandler CreateSeededSut() =>
        new(_repository, new FakeCurrentUserService(), NullLogger<RerollPseudonymCommandHandler>.Instance, new Random(Seed));

    private static Pseudonym FirstSeededDraw => PseudonymGenerator.Generate(new Random(Seed));

    [Fact]
    public async Task Should_ReplacePseudonym_When_UserIsParticipant()
    {
        _repository.Seed(Me, CurrentPseudonym, JoinedAtUtc);

        var result = await _sut.Handle(new RerollPseudonymCommand());

        Assert.True(result.IsSuccess);
        Assert.Equal(result.Pseudonym, MyProfile.Pseudonym.Value);
        Assert.Equal(JoinedAtUtc, MyProfile.JoinedAtUtc);
        Assert.Equal(1, _repository.TryUpdateCallCount);
    }

    [Fact]
    public async Task Should_ReturnDifferentPseudonym_When_Rerolling()
    {
        _repository.Seed(Me, CurrentPseudonym, JoinedAtUtc);

        // The current pseudonym is excluded from the draw, so it is never returned again.
        var previous = CurrentPseudonym;
        for (var i = 0; i < 20; i++)
        {
            var result = await _sut.Handle(new RerollPseudonymCommand());

            Assert.True(result.IsSuccess);
            Assert.NotEqual(previous, result.Pseudonym);
            previous = result.Pseudonym!;
        }
    }

    [Fact]
    public async Task Should_ReturnNotParticipating_When_ProfileIsMissing()
    {
        var result = await _sut.Handle(new RerollPseudonymCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(GamificationErrors.Gamification.NotParticipating, result.Error);
        Assert.Empty(_repository.CheckedCandidates);
        Assert.Equal(0, _repository.TryUpdateCallCount);
    }

    [Fact]
    public async Task Should_ReturnPoolExhausted_When_NoPseudonymIsAvailable()
    {
        _repository.Seed(Me, CurrentPseudonym, JoinedAtUtc);
        _repository.TakenCandidateCount = 15;

        var result = await _sut.Handle(new RerollPseudonymCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(GamificationErrors.Gamification.PseudonymPoolExhausted, result.Error);
        Assert.Equal(0, _repository.TryUpdateCallCount);
        Assert.Equal(CurrentPseudonym, MyProfile.Pseudonym.Value);
    }

    [Fact]
    public async Task Should_ReturnPoolExhausted_When_UpdateIsRejectedByUniqueIndex()
    {
        _repository.Seed(Me, CurrentPseudonym, JoinedAtUtc);
        _repository.OnTryUpdate = profile => _repository.Seed(UserId.New(), profile.Pseudonym.Value, JoinedAtUtc);

        var result = await _sut.Handle(new RerollPseudonymCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(GamificationErrors.Gamification.PseudonymPoolExhausted, result.Error);
        Assert.Equal(CurrentPseudonym, MyProfile.Pseudonym.Value);
    }

    [Fact]
    public async Task Should_ReturnNotParticipating_When_ProfileWasRemovedDuringReroll()
    {
        // A leave from another tab deletes the profile between the read and the update.
        _repository.Seed(Me, CurrentPseudonym, JoinedAtUtc);
        _repository.OnTryUpdate = profile => _repository.Delete(profile.Id);

        var result = await _sut.Handle(new RerollPseudonymCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(GamificationErrors.Gamification.NotParticipating, result.Error);
        Assert.Null(result.Pseudonym);
        Assert.Equal(1, _repository.TryUpdateCallCount);
        Assert.Empty(_repository.Stored);
    }

    [Fact]
    public async Task Should_ReplacePseudonym_When_CurrentPseudonymIsDrawnAfterConcurrentRerollFreedIt()
    {
        // The first draw is the current pseudonym, and another tab rerolls between the read and the draw:
        // the store then reports the current pseudonym as free.
        var currentPseudonym = FirstSeededDraw;
        _repository.Seed(Me, currentPseudonym.Value, JoinedAtUtc);
        var concurrentRerollDone = false;
        _repository.OnIsPseudonymTaken = _ =>
        {
            if (concurrentRerollDone)
                return;
            concurrentRerollDone = true;
            _repository.Delete(Me);
            _repository.Seed(Me, "HibouAttentif27", JoinedAtUtc);
        };

        var result = await CreateSeededSut().Handle(new RerollPseudonymCommand());

        Assert.True(result.IsSuccess);
        Assert.NotEqual(currentPseudonym.Value, result.Pseudonym);
        Assert.Equal(result.Pseudonym, MyProfile.Pseudonym.Value);
        Assert.Equal(1, _repository.TryUpdateCallCount);
    }

    [Fact]
    public async Task Should_ReturnNotParticipating_When_CurrentPseudonymIsDrawnAfterConcurrentLeaveFreedIt()
    {
        // The first draw is the current pseudonym, and another tab leaves between the read and the draw:
        // the store then reports the current pseudonym as free.
        _repository.Seed(Me, FirstSeededDraw.Value, JoinedAtUtc);
        _repository.OnIsPseudonymTaken = _ => _repository.Delete(Me);

        var result = await CreateSeededSut().Handle(new RerollPseudonymCommand());

        Assert.False(result.IsSuccess);
        Assert.Equal(GamificationErrors.Gamification.NotParticipating, result.Error);
        Assert.Null(result.Pseudonym);
        Assert.Empty(_repository.Stored);
    }
}
