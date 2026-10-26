using System.Text.RegularExpressions;
using KairuFocus.Domain.Common;

namespace KairuFocus.Domain.Gamification;

/// <summary>
/// Public name of a leaderboard participant (ADR-024). Never typed by a user:
/// <see cref="Create"/> is only called by <see cref="PseudonymGenerator"/> (and tests).
/// Invariant: ASCII letters starting with an uppercase letter, optional suffix of 1 or 2 digits,
/// at most <see cref="MaxLength"/> characters ("LoutreTenace", "LoutreTenace27").
/// </summary>
public sealed partial record Pseudonym
{
    public const int MaxLength = 60;

    public string Value { get; }

    private Pseudonym(string value) => Value = value;

    public static Result<Pseudonym> Create(string value)
    {
        if (string.IsNullOrEmpty(value) || value.Length > MaxLength || !Format().IsMatch(value))
            return Result.Failure<Pseudonym>(DomainErrors.Gamification.InvalidPseudonym);

        return Result.Success(new Pseudonym(value));
    }

    /// <summary>Restores a pseudonym from a trusted stored value (EF Core / DB) — bypasses validation.</summary>
    public static Pseudonym Restore(string storedValue) => new(storedValue);

    public override string ToString() => Value;

    // \z instead of $: $ would also accept a trailing "\n".
    // No IgnoreCase: character ranges stay strictly ASCII.
    [GeneratedRegex(@"^[A-Z][A-Za-z]+[0-9]{0,2}\z", RegexOptions.CultureInvariant)]
    private static partial Regex Format();
}
