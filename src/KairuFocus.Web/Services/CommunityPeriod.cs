namespace KairuFocus.Web.Services;

/// <summary>Period of the Community page. The enum member names double as resource-key suffixes.</summary>
public enum CommunityPeriod
{
    Week,
    Month,
    Global
}

public static class CommunityPeriodExtensions
{
    /// <summary>Value of the <c>period</c> query parameter of the API.</summary>
    public static string ToApiValue(this CommunityPeriod period) => period switch
    {
        CommunityPeriod.Month => "month",
        CommunityPeriod.Global => "global",
        _ => "week"
    };

    /// <summary>Value of the <c>periode</c> query parameter of the page URL.</summary>
    public static string ToUrlValue(this CommunityPeriod period) => period switch
    {
        CommunityPeriod.Month => "mois",
        CommunityPeriod.Global => "global",
        _ => "semaine"
    };

    /// <summary>Parses the page URL value; anything unknown or missing falls back to the week.</summary>
    public static CommunityPeriod FromUrlValue(string? value) => value?.ToLowerInvariant() switch
    {
        "mois" => CommunityPeriod.Month,
        "global" => CommunityPeriod.Global,
        _ => CommunityPeriod.Week
    };
}
