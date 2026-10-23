using KairuFocus.Domain.Gamification;

namespace KairuFocus.Application.Gamification.Common;

/// <summary>
/// Draws a free pseudonym: <see cref="PlainAttempts"/> plain draws ("LoutreTenace"), then
/// <see cref="SuffixedAttempts"/> draws with a random suffix 2–99 ("LoutreTenace27"). Each candidate
/// is checked with <see cref="ILeaderboardProfileRepository.IsPseudonymTakenAsync"/>. The unique index
/// remains the last line of defense against a concurrent draw.
/// </summary>
internal static class PseudonymAllocator
{
    public const int PlainAttempts = 5;
    public const int SuffixedAttempts = 10;

    /// <param name="excluded">
    /// Candidate rejected without asking the repository (it consumes an attempt), or null. A reroll passes the
    /// pseudonym of the profile it loaded: the stored row may have changed since the read (a concurrent reroll
    /// or leave frees it), so the repository alone cannot guarantee that the current pseudonym is not drawn again.
    /// </param>
    /// <returns>A free pseudonym, or null when every candidate was taken.</returns>
    public static async Task<Pseudonym?> AllocateAsync(
        ILeaderboardProfileRepository repository,
        Random random,
        Pseudonym? excluded,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < PlainAttempts; attempt++)
        {
            var candidate = PseudonymGenerator.Generate(random);
            if (candidate != excluded && !await repository.IsPseudonymTakenAsync(candidate, cancellationToken))
                return candidate;
        }

        for (var attempt = 0; attempt < SuffixedAttempts; attempt++)
        {
            var suffix = random.Next(PseudonymGenerator.MinSuffix, PseudonymGenerator.MaxSuffix + 1);
            var candidate = PseudonymGenerator.GenerateWithSuffix(random, suffix);
            if (candidate != excluded && !await repository.IsPseudonymTakenAsync(candidate, cancellationToken))
                return candidate;
        }

        return null;
    }
}
