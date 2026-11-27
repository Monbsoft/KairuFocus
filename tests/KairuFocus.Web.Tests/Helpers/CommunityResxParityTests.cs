using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace KairuFocus.Web.Tests.Helpers;

/// <summary>
/// Checks that the translated <c>Community.{lang}.resx</c> files carry the same keys and placeholders
/// as the French default <c>Community.resx</c>, for every key.
/// </summary>
public sealed class CommunityResxParityTests
{
    private static readonly Regex Placeholder = new(@"\{(\d+)(?::[^}]*)?\}", RegexOptions.CultureInvariant);
    private static readonly Lazy<string> PagesDirectory = new(FindPagesDirectory);

    // Same lookup as StaticSiteFiles: walk up from the test binaries until KairuFocus.slnx is found.
    private static string FindPagesDirectory()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "KairuFocus.slnx")))
                return Path.Combine(dir.FullName, "src", "KairuFocus.Web", "Pages");
        }

        throw new InvalidOperationException($"KairuFocus.slnx not found above {AppContext.BaseDirectory}.");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("pt")]
    [InlineData("it")]
    [InlineData("ru")]
    [InlineData("ja")]
    [InlineData("ko")]
    [InlineData("zh")]
    public void Should_HaveSameKeys_When_ComparingToFrench(string lang)
    {
        var french = LoadValues("Community.resx");
        var translated = LoadValues($"Community.{lang}.resx");

        var missing = french.Keys.Except(translated.Keys).OrderBy(k => k, StringComparer.Ordinal).ToList();
        var extra = translated.Keys.Except(french.Keys).OrderBy(k => k, StringComparer.Ordinal).ToList();

        Assert.True(
            missing.Count == 0 && extra.Count == 0,
            $"[{lang}] missing keys: [{string.Join(", ", missing)}]; extra keys: [{string.Join(", ", extra)}]");
    }

    [Theory]
    [InlineData("en")]
    [InlineData("de")]
    [InlineData("es")]
    [InlineData("pt")]
    [InlineData("it")]
    [InlineData("ru")]
    [InlineData("ja")]
    [InlineData("ko")]
    [InlineData("zh")]
    public void Should_HaveSamePlaceholders_When_ComparingToFrench(string lang)
    {
        var french = LoadValues("Community.resx");
        var translated = LoadValues($"Community.{lang}.resx");

        var offending = french
            .Where(kv => translated.ContainsKey(kv.Key))
            .Where(kv => !PlaceholdersAccepted(kv.Key, french, kv.Value, translated[kv.Key]))
            .Select(kv => kv.Key)
            .OrderBy(k => k, StringComparer.Ordinal)
            .ToList();

        Assert.True(
            offending.Count == 0,
            $"[{lang}] placeholder mismatch vs French on keys: [{string.Join(", ", offending)}]");
    }

    // Single plural form languages (ja, ko, zh) have no distinct "one" form: they copy the "other" text,
    // so a ".One" key may legitimately carry the placeholders of the French ".Other" sibling.
    private static bool PlaceholdersAccepted(
        string key,
        Dictionary<string, string> french,
        string frenchValue,
        string translatedValue)
    {
        if (SamePlaceholders(frenchValue, translatedValue))
            return true;

        if (!key.EndsWith(".One", StringComparison.Ordinal))
            return false;

        var otherKey = key[..^".One".Length] + ".Other";
        if (!french.TryGetValue(otherKey, out var frenchOther))
            return false;

        var allowed = Placeholders(frenchOther).ToHashSet(StringComparer.Ordinal);
        return Placeholders(translatedValue).All(allowed.Contains);
    }

    private static IEnumerable<string> Placeholders(string value) =>
        Placeholder.Matches(value).Select(m => "{" + m.Groups[1].Value + "}");

    private static bool SamePlaceholders(string frenchValue, string translatedValue)
    {
        var expected = Placeholders(frenchValue).OrderBy(v => v, StringComparer.Ordinal).ToList();
        var actual = Placeholders(translatedValue).OrderBy(v => v, StringComparer.Ordinal).ToList();
        return expected.SequenceEqual(actual);
    }

    private static Dictionary<string, string> LoadValues(string fileName)
    {
        var document = XDocument.Load(Path.Combine(PagesDirectory.Value, fileName));

        return document.Root!
            .Elements("data")
            .ToDictionary(
                e => (string)e.Attribute("name")!,
                e => (string?)e.Element("value") ?? string.Empty,
                StringComparer.Ordinal);
    }
}
