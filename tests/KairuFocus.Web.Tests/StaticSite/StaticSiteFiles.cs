namespace KairuFocus.Web.Tests.StaticSite;

/// <summary>
/// Reads the files of <c>src/KairuFocus.Web/wwwroot</c> as the API serves them, before any JavaScript runs:
/// crawlers that do not execute JS only ever see these.
/// </summary>
internal static class StaticSiteFiles
{
    public const string CanonicalUrl = "https://kairufocus.com/";
    public const string ShareImageUrl = "https://kairufocus.com/og-image.png";

    private static readonly Lazy<string> WwwRoot = new(FindWwwRoot);

    public static string PathOf(string fileName) => Path.Combine(WwwRoot.Value, fileName);

    public static string ReadText(string fileName) => File.ReadAllText(PathOf(fileName));

    private static string FindWwwRoot()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            if (File.Exists(Path.Combine(dir.FullName, "KairuFocus.slnx")))
                return Path.Combine(dir.FullName, "src", "KairuFocus.Web", "wwwroot");
        }

        throw new InvalidOperationException($"KairuFocus.slnx not found above {AppContext.BaseDirectory}.");
    }
}
