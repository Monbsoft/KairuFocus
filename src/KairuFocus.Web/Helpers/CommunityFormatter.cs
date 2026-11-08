using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;
using KairuFocus.Web.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace KairuFocus.Web.Helpers;

/// <summary>
/// Texts and culture-aware formats of the Community page and of the community section of the
/// settings. Wraps <see cref="IStringLocalizer{Community}"/> (resource <c>Pages/Community.resx</c>)
/// and holds the rules a resource file cannot express: plural forms, ordinals, durations, dates.
/// <para>
/// Plural forms use <c>key.One</c> / <c>key.Other</c> resources. Date formats are resources too
/// (<c>Community.Format.*</c>), because their order differs per language.
/// </para>
/// </summary>
public sealed class CommunityFormatter
{
    private readonly IStringLocalizer<Community> _localizer;
    private readonly TimeProvider _clock;

    public CommunityFormatter(IStringLocalizer<Community> localizer, TimeProvider clock)
    {
        _localizer = localizer;
        _clock = clock;
    }

    private static CultureInfo Culture => CultureInfo.CurrentCulture;

    private static string Language => CultureInfo.CurrentUICulture.TwoLetterISOLanguageName;

    // ── Texts ───────────────────────────────────────────────────────────────

    /// <summary>The text of a resource key (the key itself when the resource is missing).</summary>
    public string this[string key] => _localizer[key].Value;

    /// <summary>The text of a resource key, formatted with the given arguments.</summary>
    public string Format(string key, params object[] args) => _localizer[key, args].Value;

    // ── Numbers and plurals ─────────────────────────────────────────────────

    public string Number(int value) => value.ToString("N0", Culture);

    public string Xp(int value) => $"{Number(value)} XP";

    /// <summary>French and Portuguese use the singular for 0 and 1 (CLDR: i = 0,1); the others for 1 only.
    /// Languages without a plural (ja, ko, zh) and Russian carry the same text in both forms.</summary>
    private static bool UsesSingular(int count) => Language switch
    {
        "fr" or "pt" => count is 0 or 1,
        _ => count == 1
    };

    /// <summary>The template of <c>key.One</c> or <c>key.Other</c>, chosen from the count.</summary>
    public string PluralTemplate(string key, int count) => this[$"{key}.{(UsesSingular(count) ? "One" : "Other")}"];

    /// <summary>The plural form chosen from <paramref name="count"/>, formatted with <paramref name="args"/>.</summary>
    public string Plural(string key, int count, params object[] args) =>
        string.Format(Culture, PluralTemplate(key, count), args);

    // ── Emphasis ────────────────────────────────────────────────────────────

    /// <summary>The template with <c>{0}</c> replaced by <paramref name="value"/> in bold. Everything is HTML-encoded.</summary>
    public MarkupString Bold(string template, string value) =>
        new(WebUtility.HtmlEncode(template).Replace("{0}", $"<b>{WebUtility.HtmlEncode(value)}</b>", StringComparison.Ordinal));

    /// <summary>The plural form with the count in bold.</summary>
    public MarkupString BoldPlural(string key, int count) => Bold(PluralTemplate(key, count), Number(count));

    // ── Links in a sentence ─────────────────────────────────────────────────

    // ASCII digits only (\d would also match other scripts' digits), and short enough to always parse.
    private static readonly Regex Placeholder = new(@"\{([0-9]{1,3})\}", RegexOptions.CultureInvariant);

    /// <summary>
    /// The template with each <c>{i}</c> replaced by <paramref name="args"/>[i]: a link when it has an address, plain
    /// text otherwise. One pass, so a name that looks like a placeholder stays a name. Everything is HTML-encoded.
    /// The links carry no class: style them from the component with <c>::deep a</c>.
    /// </summary>
    public static MarkupString WithLinks(string template, params (string Text, string? Href)[] args) =>
        Linked(template, newTabText: null, args);

    /// <summary>
    /// Same as <see cref="WithLinks"/>, the links opening in a new tab, so that the page showing them keeps running (the
    /// Pomodoro page plays the ringtone, records the end of the sprint and asks for the group break). A small arrow says
    /// it on screen; <paramref name="newTabText"/> says it to screen readers, after each name.
    /// </summary>
    public static MarkupString WithNewTabLinks(string template, string newTabText, params (string Text, string? Href)[] args) =>
        Linked(template, newTabText, args);

    private static MarkupString Linked(string template, string? newTabText, (string Text, string? Href)[] args) =>
        new(Placeholder.Replace(WebUtility.HtmlEncode(template), match =>
        {
            if (!int.TryParse(match.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var index)
                || index >= args.Length)
                return match.Value;

            var (text, href) = args[index];
            var value = WebUtility.HtmlEncode(text);
            if (href is null) return value;

            var address = WebUtility.HtmlEncode(href);
            return newTabText is null
                ? $"<a href=\"{address}\">{value}</a>"
                : $"<a href=\"{address}\" target=\"_blank\" rel=\"noopener\">{value}<span aria-hidden=\"true\"> ↗</span>"
                  + $"<span class=\"visually-hidden\"> {WebUtility.HtmlEncode(newTabText)}</span></a>";
        }));

    /// <summary>The address of a member's public profile.</summary>
    public static string ProfileHref(string name) => $"/membres/{Uri.EscapeDataString(name)}";

    /// <summary>
    /// The template cut around its first <c>{0}</c>, so that a component can render its own markup there (a countdown,
    /// a styled figure); null when there is none (a missing resource comes back as its key). The caller picks the fallback.
    /// </summary>
    public static (string Before, string After)? SplitAtPlaceholder(string template)
    {
        var at = template.IndexOf("{0}", StringComparison.Ordinal);
        return at < 0 ? null : (template[..at], template[(at + 3)..]);
    }

    // ── Ordinals ────────────────────────────────────────────────────────────

    /// <summary>
    /// Localized ordinal (« 1er », « 4e », « 1st », « 11th », « 4. », « 4.º », « 4-й », « 4位 », « 第4名 »…), in three parts.
    /// The number is formatted on its own and the marker is what the resource carries around <c>{0}</c>: the page can then
    /// set the figure in its large monospace face and the marker in a small proportional one, instead of letting every
    /// letter of the marker take a monospace cell.
    /// </summary>
    public OrdinalParts Ordinal(int value)
    {
        // No placeholder (resource missing: the localizer returns the key): the bare number is still right.
        var (prefix, suffix) = SplitAtPlaceholder(this[$"Community.Ordinal.{OrdinalCategory(value)}"]) ?? ("", "");
        return new OrdinalParts(prefix, Number(value), suffix);
    }

    private static string OrdinalCategory(int value)
    {
        if (Language == "en")
        {
            if (value % 100 is >= 11 and <= 13) return "Other";
            return (value % 10) switch
            {
                1 => "One",
                2 => "Two",
                3 => "Few",
                _ => "Other"
            };
        }

        return value == 1 ? "One" : "Other";
    }

    // ── Durations ───────────────────────────────────────────────────────────

    /// <summary>« 45 min », « 5 h », « 13 h 20 » (fr) / « 13 h 20 min » (en). The minutes are passed as a number: whether
    /// they are zero-padded (« 1 h 05 ») is the resource's business (<c>{1:00}</c> or <c>{1}</c>), as it depends on the language.</summary>
    public string Duration(int minutes)
    {
        if (minutes < 60) return Format("Community.Duration.Minutes", Number(minutes));

        var hours = minutes / 60;
        var rest = minutes % 60;
        return rest == 0
            ? Format("Community.Duration.Hours", Number(hours))
            : Format("Community.Duration.HoursMinutes", Number(hours), rest);
    }

    /// <summary>Focus time of the collective recap: rounded to the hour, « moins d'1 h » under an hour, « 0 h » for none.</summary>
    public string RecapFocus(int minutes)
    {
        if (minutes <= 0) return Format("Community.Duration.Hours", Number(0));
        if (minutes < 60) return this["Community.Together.LessThanHour"];

        var hours = (int)Math.Round(minutes / 60.0, MidpointRounding.AwayFromZero);
        return Format("Community.Duration.Hours", Number(hours));
    }

    // ── Dates ───────────────────────────────────────────────────────────────
    // A date shows its year only when it is not the current one.

    // Same clock as the page and the hero (UTC): a label never contradicts the dates they compute.
    private int CurrentYear => _clock.GetUtcNow().Year;

    /// <summary>« 21 septembre » (with the year when it is not the current one).</summary>
    public string DayMonth(DateOnly date) =>
        date.ToString(this[date.Year == CurrentYear ? "Community.Format.DayMonth" : "Community.Format.DayMonthYear"], Culture);

    /// <summary>« lundi 12 octobre ».</summary>
    public string WeekdayDayMonth(DateOnly date) => date.ToString(this["Community.Format.WeekdayDayMonth"], Culture);

    /// <summary>« 24/8 » (order and separator depend on the culture).</summary>
    public string ShortDayMonth(DateOnly date) => date.ToString(this["Community.Format.ShortDayMonth"], Culture);

    /// <summary>« septembre » (with the year when it is not the current one).</summary>
    public string MonthName(DateOnly date) =>
        date.ToString(this[date.Year == CurrentYear ? "Community.Format.Month" : "Community.Format.MonthYear"], Culture);

    /// <summary>« juil. » (with the year when it is not the current one: « juil. 2026 »).</summary>
    public string MonthShort(DateOnly date) =>
        date.Year == CurrentYear ? date.ToString("MMM", Culture) : date.ToString(this["Community.Format.MonthShortYear"], Culture);

    /// <summary>The weekday of an instant, in local time (« vendredi »).</summary>
    public string WeekdayName(DateTimeOffset instant) =>
        Culture.DateTimeFormat.GetDayName(instant.ToLocalTime().DayOfWeek);

    /// <summary>The local calendar date of an instant.</summary>
    public static DateOnly LocalDate(DateTimeOffset instant) => DateOnly.FromDateTime(instant.ToLocalTime().DateTime);

    public static string Capitalize(string text) =>
        text.Length == 0 ? text : char.ToUpper(text[0], Culture) + text[1..];
}

/// <summary>An ordinal cut at the number: what the resource puts before it (« 第 »), the formatted number, and what follows (« er », « th », « 位 »).</summary>
public readonly record struct OrdinalParts(string Prefix, string Number, string Suffix)
{
    /// <summary>How typography sets the marker, deduced from its characters (so a resource never has to say it).</summary>
    public OrdinalMarkerStyle MarkerStyle =>
        Suffix.Length > 0 && Suffix.All(char.IsAsciiLetter) ? OrdinalMarkerStyle.Raised
        : Suffix.Length > 0 && Suffix.All(c => c is '.' or 'º' or 'ª') ? OrdinalMarkerStyle.FigureSized
        : OrdinalMarkerStyle.Small;
}

public enum OrdinalMarkerStyle
{
    /// <summary>Plain letters (« er », « e », « st », « th »): small and raised, like a superscript.</summary>
    Raised,

    /// <summary>Marks drawn to stand next to a figure at its own size (« . », « º », « .º »): not shrunk. « º » is already a raised glyph, and a raised bare period would be wrong.</summary>
    FigureSized,

    /// <summary>Anything else (« -й », « 位 », « 위 », « 第 », « 名 »): small, on the baseline, as these scripts write it.</summary>
    Small
}
