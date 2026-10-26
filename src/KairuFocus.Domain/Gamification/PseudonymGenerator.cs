using System.Globalization;

namespace KairuFocus.Domain.Gamification;

/// <summary>
/// Draws a leaderboard pseudonym: a French animal followed by a quality agreed in gender
/// with the animal ("HibouAttentif", "LoutreAttentive"), optionally suffixed ("LoutreTenace27").
/// Pure: the caller provides the <see cref="Random"/> source; no I/O, no clock.
/// The lists are stable business data, hard-coded on purpose. They are written in ASCII
/// without accents and are identical in every UI language.
/// </summary>
public static class PseudonymGenerator
{
    public const int MinSuffix = 2;
    public const int MaxSuffix = 99;

    private const bool Feminine = true;
    private const bool Masculine = false;

    /// <summary>Animals with the grammatical gender of their French name.</summary>
    internal static IReadOnlyList<(string Name, bool IsFeminine)> Animals { get; } =
    [
        ("Abeille", Feminine),
        ("Albatros", Masculine),
        ("Alouette", Feminine),
        ("Antilope", Feminine),
        ("Baleine", Feminine),
        ("Biche", Feminine),
        ("Bison", Masculine),
        ("Bouquetin", Masculine),
        ("Castor", Masculine),
        ("Cerf", Masculine),
        ("Chamois", Masculine),
        ("Chevreuil", Masculine),
        ("Chouette", Feminine),
        ("Cigogne", Feminine),
        ("Coccinelle", Feminine),
        ("Colibri", Masculine),
        ("Colombe", Feminine),
        ("Cygne", Masculine),
        ("Dauphin", Masculine),
        ("Ecureuil", Masculine),
        ("Faon", Masculine),
        ("Faucon", Masculine),
        ("Fauvette", Feminine),
        ("Fennec", Masculine),
        ("Fourmi", Feminine),
        ("Gazelle", Feminine),
        ("Girafe", Feminine),
        ("Herisson", Masculine),
        ("Hermine", Feminine),
        ("Heron", Masculine),
        ("Hibou", Masculine),
        ("Hirondelle", Feminine),
        ("Koala", Masculine),
        ("Libellule", Feminine),
        ("Loup", Masculine),
        ("Loutre", Feminine),
        ("Luciole", Feminine),
        ("Lynx", Masculine),
        ("Mangouste", Feminine),
        ("Merle", Masculine),
        ("Mesange", Feminine),
        ("Moineau", Masculine),
        ("Narval", Masculine),
        ("Ocelot", Masculine),
        ("Otarie", Feminine),
        ("Ours", Masculine),
        ("Panda", Masculine),
        ("Panthere", Feminine),
        ("Pingouin", Masculine),
        ("Pinson", Masculine),
        ("Renard", Masculine),
        ("Salamandre", Feminine),
        ("Tigre", Masculine),
        ("Tortue", Feminine),
        ("Toucan", Masculine),
        ("Tourterelle", Feminine),
    ];

    /// <summary>Qualities with their masculine and feminine forms (identical for epicene adjectives).</summary>
    internal static IReadOnlyList<(string Masculine, string Feminine)> Qualities { get; } =
    [
        ("Agile", "Agile"),
        ("Assidu", "Assidue"),
        ("Astucieux", "Astucieuse"),
        ("Attentif", "Attentive"),
        ("Calme", "Calme"),
        ("Clairvoyant", "Clairvoyante"),
        ("Constant", "Constante"),
        ("Contemplatif", "Contemplative"),
        ("Doux", "Douce"),
        ("Endurant", "Endurante"),
        ("Fiable", "Fiable"),
        ("Habile", "Habile"),
        ("Imperturbable", "Imperturbable"),
        ("Infatigable", "Infatigable"),
        ("Inventif", "Inventive"),
        ("Lucide", "Lucide"),
        ("Minutieux", "Minutieuse"),
        ("Paisible", "Paisible"),
        ("Patient", "Patiente"),
        ("Pensif", "Pensive"),
        ("Perspicace", "Perspicace"),
        ("Placide", "Placide"),
        ("Prudent", "Prudente"),
        ("Rigoureux", "Rigoureuse"),
        ("Sage", "Sage"),
        ("Serein", "Sereine"),
        ("Silencieux", "Silencieuse"),
        ("Soigneux", "Soigneuse"),
        ("Stable", "Stable"),
        ("Studieux", "Studieuse"),
        ("Tenace", "Tenace"),
        ("Tranquille", "Tranquille"),
        ("Vif", "Vive"),
        ("Zen", "Zen"),
    ];

    /// <summary>Draws an animal then a quality: "LoutreTenace".</summary>
    public static Pseudonym Generate(Random random)
    {
        ArgumentNullException.ThrowIfNull(random);

        return Draw(random, suffix: null);
    }

    /// <summary>
    /// Same draw as <see cref="Generate"/> followed by a numeric suffix: "LoutreTenace27".
    /// Throws when <paramref name="suffix"/> is outside [<see cref="MinSuffix"/>, <see cref="MaxSuffix"/>]
    /// (programmer error).
    /// </summary>
    public static Pseudonym GenerateWithSuffix(Random random, int suffix)
    {
        ArgumentNullException.ThrowIfNull(random);
        ArgumentOutOfRangeException.ThrowIfLessThan(suffix, MinSuffix);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(suffix, MaxSuffix);

        return Draw(random, suffix);
    }

    /// <summary>Builds the pseudonym of one combination. Exposed to tests to walk every combination.</summary>
    internal static Pseudonym Compose(int animalIndex, int qualityIndex, int? suffix = null)
    {
        var (name, isFeminine) = Animals[animalIndex];
        var (masculine, feminine) = Qualities[qualityIndex];
        var value = name + (isFeminine ? feminine : masculine) + suffix?.ToString(CultureInfo.InvariantCulture);

        var result = Pseudonym.Create(value);
        if (result.IsFailure)
            throw new InvalidOperationException($"The curated lists produced an invalid pseudonym: '{value}'.");

        return result.Value;
    }

    private static Pseudonym Draw(Random random, int? suffix)
    {
        var animalIndex = random.Next(Animals.Count);
        var qualityIndex = random.Next(Qualities.Count);

        return Compose(animalIndex, qualityIndex, suffix);
    }
}
