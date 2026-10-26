using System.Text.RegularExpressions;
using KairuFocus.Application.Gamification.Common;
using KairuFocus.Domain.Gamification;

namespace KairuFocus.Application.Tests.Gamification;

public sealed class PseudonymAllocatorTests
{
    private const int Seed = 42;
    private static readonly Regex NumericSuffix = new(@"[0-9]{1,2}\z", RegexOptions.CultureInvariant);

    private readonly FakeLeaderboardProfileRepository _repository = new();

    private Task<Pseudonym?> AllocateAsync(Pseudonym? excluded = null) =>
        PseudonymAllocator.AllocateAsync(_repository, new Random(Seed), excluded, CancellationToken.None);

    [Fact]
    public async Task Should_SkipExcludedPseudonym_When_ItIsDrawnAndReportedFree()
    {
        // The first draw of the seeded random is the excluded pseudonym; the store reports it free.
        var excluded = PseudonymGenerator.Generate(new Random(Seed));

        var pseudonym = await AllocateAsync(excluded);

        Assert.NotNull(pseudonym);
        Assert.NotEqual(excluded, pseudonym);
        Assert.DoesNotContain(excluded, _repository.CheckedCandidates);
    }

    [Fact]
    public async Task Should_ReturnFirstCandidate_When_ItIsFree()
    {
        var pseudonym = await AllocateAsync();

        Assert.NotNull(pseudonym);
        var checkedCandidate = Assert.Single(_repository.CheckedCandidates);
        Assert.Equal(checkedCandidate, pseudonym);
    }

    [Fact]
    public async Task Should_TryAnotherCandidate_When_FirstIsTaken()
    {
        _repository.TakenCandidateCount = 1;

        var pseudonym = await AllocateAsync();

        Assert.Equal(2, _repository.CheckedCandidates.Count);
        Assert.Equal(_repository.CheckedCandidates[1], pseudonym);
    }

    [Fact]
    public async Task Should_UseNumericSuffix_When_FivePlainCandidatesAreTaken()
    {
        _repository.TakenCandidateCount = PseudonymAllocator.PlainAttempts;

        var pseudonym = await AllocateAsync();

        Assert.NotNull(pseudonym);
        Assert.Equal(PseudonymAllocator.PlainAttempts + 1, _repository.CheckedCandidates.Count);
        Assert.All(
            _repository.CheckedCandidates.Take(PseudonymAllocator.PlainAttempts),
            plain => Assert.DoesNotMatch(NumericSuffix, plain.Value));

        var suffix = int.Parse(NumericSuffix.Match(pseudonym.Value).Value);
        Assert.InRange(suffix, 2, 99);
    }

    [Fact]
    public async Task Should_ReturnNull_When_AllFifteenCandidatesAreTaken()
    {
        _repository.TakenCandidateCount = PseudonymAllocator.PlainAttempts + PseudonymAllocator.SuffixedAttempts;

        var pseudonym = await AllocateAsync();

        Assert.Null(pseudonym);
        Assert.Equal(15, _repository.CheckedCandidates.Count);
    }
}
