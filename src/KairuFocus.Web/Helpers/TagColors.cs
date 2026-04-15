namespace KairuFocus.Web.Helpers;

public static class TagColors
{
    private static readonly string[] Palette =
    [
        "bg-primary",
        "bg-success",
        "bg-danger",
        "bg-warning text-dark",
        "bg-info text-dark",
        "bg-secondary",
        "text-bg-purple",
        "text-bg-orange",
        "text-bg-teal",
        "text-bg-pink"
    ];

    public static string GetClass(string tag)
    {
        var normalized = tag.ToLowerInvariant();
        uint hash = 5381;
        foreach (var c in normalized)
            hash = hash * 33 ^ c;
        return Palette[hash % (uint)Palette.Length];
    }
}
