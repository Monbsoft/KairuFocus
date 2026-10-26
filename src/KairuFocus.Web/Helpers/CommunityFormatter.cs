using System.Globalization;
using System.Net;
using KairuFocus.Web.Pages;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;

namespace KairuFocus.Web.Helpers;

/// <summary>
/// Texts and culture-aware formats of the Community page and of the leaderboard section of the
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

    // ── Ordinals ────────────────────────────────────────────────────────────

    /// <summary>Localized ordinal: « 1ᵉʳ », « 4ᵉ », « 1st », « 2nd », « 11th », « 4. », « 第4名 »…</summary>
    public string Ordinal(int value) => Format($"Community.Ordinal.{OrdinalCategory(value)}", Number(value));

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

    /// <summary>« 45 min », « 5 h », « 13 h 20 ».</summary>
    public string Duration(int minutes)
    {
        if (minutes < 60) return Format("Community.Duration.Minutes", Number(minutes));

        var hours = minutes / 60;
        var rest = minutes % 60;
        return rest == 0
            ? Format("Community.Duration.Hours", Number(hours))
            : Format("Community.Duration.HoursMinutes", Number(hours), rest.ToString("D2", Culture));
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
