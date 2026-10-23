using KairuFocus.Domain.Gamification;

namespace KairuFocus.Application.Gamification.Common;

/// <summary>
/// Draws a free pseudonym: <see cref="PlainAttempts"/> plain draws ("LoutreTenace"), then
/// <see cref="SuffixedAttempts"/> draws with a random suffix 2–99 ("LoutreTenace27"). Each candidate
/// is checked with <see cref="ILeaderboardProfileRepository.IsPseudonymTakenAsync"/>; a pseudonym held
/// by the caller is taken too, so a reroll never returns the current one. The unique index remains
/// the last line of defense against a concurrent draw.
/// </summary>
internal static class PseudonymAllocator
{
    public const int PlainAttempts = 5;
    public const int SuffixedAttempts = 10;

    /// <returns>A free pseudonym, or null when every candidate was taken.</returns>
    public static async Task<Pseudonym?> AllocateAsync(
        ILeaderboardProfileRepository repository,
        Random random,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < PlainAttempts; attempt++)
        {
            var candidate = PseudonymGenerator.Generate(random);
            if (!await repository.IsPseudonymTakenAsync(candidate, cancellationToken))
                return candidate;
        }

        for (var attempt = 0; attempt < SuffixedAttempts; attempt++)
        {
            var suffix = random.Next(PseudonymGenerator.MinSuffix, PseudonymGenerator.MaxSuffix + 1);
            var candidate = PseudonymGenerator.GenerateWithSuffix(random, suffix);
            if (!await repository.IsPseudonymTakenAsync(candidate, cancellationToken))
                return candidate;
        }

        return null;
    }
}
