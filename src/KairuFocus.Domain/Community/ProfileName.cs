using System.Text.RegularExpressions;
using KairuFocus.Domain.Common;

namespace KairuFocus.Domain.Community;

/// <summary>
/// Public name of a community member: a GitHub login or a name chosen by the member.
/// Invariant: 3 to 39 ASCII letters, digits, '-' or '_', starting with a letter or a digit, and not a
/// reserved word. Uniqueness is case-insensitive and carried by <see cref="Key"/>, so that it does not
/// depend on the database collation.
/// </summary>
public sealed partial record ProfileName
{
    public const int MinLength = 3;
    public const int MaxLength = 39;

    // Compared on Key. "me" is below MinLength: the format rejects it first, it is listed so that
    // this set stays the full list of reserved words.
    private static readonly HashSet<string> ReservedKeys = new(StringComparer.Ordinal)
    {
        "admin", "kairu", "kairufocus", "support", "moi", "me", "profil", "membres", "null", "undefined",
    };

    public string Value { get; }

    /// <summary>Case-insensitive identity of the name: <see cref="Value"/> in lowercase.</summary>
    public string Key { get; }

    private ProfileName(string value)
    {
        Value = value;
        Key = value.ToLowerInvariant();
    }

    public static Result<ProfileName> Create(string? value)
    {
        if (value is null || !Format().IsMatch(value))
            return Result.Failure<ProfileName>(DomainErrors.Community.InvalidName);

        var name = new ProfileName(value);

        return ReservedKeys.Contains(name.Key)
            ? Result.Failure<ProfileName>(DomainErrors.Community.ReservedName)
            : Result.Success(name);
    }

    /// <summary>Restores a name from a trusted stored value (EF Core / DB) — bypasses validation.</summary>
    public static ProfileName Restore(string storedValue) => new(storedValue);

    public override string ToString() => Value;

    // First character + {MinLength - 1, MaxLength - 1} others.
    // \z instead of $: $ would also accept a trailing "\n".
    // No IgnoreCase: character ranges stay strictly ASCII.
    [GeneratedRegex(@"^[A-Za-z0-9][A-Za-z0-9_-]{2,38}\z", RegexOptions.CultureInvariant)]
    private static partial Regex Format();
}
