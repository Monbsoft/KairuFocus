using KairuFocus.Domain.Community;

namespace KairuFocus.Application.Community.Common;

/// <param name="Category">An <see cref="AmbianceCategory"/> name.</param>
/// <param name="Url">The YouTube video of the ambiance, resolved from the catalogue (never stored).</param>
public sealed record AmbianceViewModel(string Code, string Label, string Category, string Url)
{
    public static AmbianceViewModel From(Ambiance ambiance) =>
        new(ambiance.Code, ambiance.Label, ambiance.Category.ToString(), ambiance.VideoUrl);

    /// <summary>
    /// The ambiance of <paramref name="code"/> in <paramref name="catalogue"/>; null when none, or when the code
    /// left the catalogue (it then counts as no ambiance).
    /// </summary>
    public static AmbianceViewModel? Resolve(string? code, AmbianceCatalogue catalogue) =>
        catalogue.Find(code) is { } ambiance ? From(ambiance) : null;
}
