namespace KairuFocus.Web.Helpers;

public static class SupportedCultures
{
    public static IReadOnlyList<(string Code, string Label)> All { get; } =
    [
        ("fr", "🇫🇷 Français"),
        ("en", "🇬🇧 English"),
        ("de", "🇩🇪 Deutsch"),
        ("es", "🇪🇸 Español"),
        ("pt", "🇧🇷 Português"),
        ("it", "🇮🇹 Italiano"),
        ("ru", "🇷🇺 Русский"),
        ("ja", "🇯🇵 日本語"),
        ("ko", "🇰🇷 한국어"),
        ("zh", "🇨🇳 中文"),
    ];

    public static string LabelFor(string code)
    {
        foreach (var culture in All)
        {
            if (culture.Code == code) return culture.Label;
        }

        return All[0].Label;
    }
}
