using KairuFocus.Application.Common;
using KairuFocus.Domain.Community;

namespace KairuFocus.Application.Community.Common;

public static class ProfileNameSources
{
    /// <summary>
    /// Parses the <c>source</c> field of an HTTP payload: only the exact name of a member, case-insensitively.
    /// Numeric values, lists and padded names are rejected (see <see cref="EnumName"/>, which is internal to
    /// Application, hence this public entry point, like <c>CommunityPeriods</c>).
    /// </summary>
    public static bool TryParse(string? value, out ProfileNameSource source) =>
        EnumName.TryParse(value, out source);
}
