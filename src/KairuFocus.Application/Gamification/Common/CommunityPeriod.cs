using KairuFocus.Application.Common;

namespace KairuFocus.Application.Gamification.Common;

/// <summary>Period shown by the community page. Never serialized: results carry dates, not this enum.</summary>
public enum CommunityPeriod
{
    Week,
    Month,
    Global
}

public static class CommunityPeriods
{
    /// <summary>
    /// Parses the <c>period</c> query parameter. Missing (null or empty) → <see cref="CommunityPeriod.Week"/>,
    /// the default. Otherwise only the exact name of a member, case-insensitively: "7", "-1", "Week,Month"
    /// or " week " are rejected (see <see cref="EnumName"/>).
    /// </summary>
    public static bool TryParse(string? value, out CommunityPeriod period)
    {
        if (string.IsNullOrEmpty(value))
        {
            period = CommunityPeriod.Week;
            return true;
        }

        return EnumName.TryParse(value, out period);
    }
}
