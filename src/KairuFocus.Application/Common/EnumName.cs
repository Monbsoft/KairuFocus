namespace KairuFocus.Application.Common;

/// <summary>
/// Parses a string coming from outside the application (API payload) into an enum member,
/// accepting exactly one member name, case-insensitively, and nothing else.
/// </summary>
/// <remarks>
/// <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/> alone is too permissive for
/// validating input: it also accepts numeric strings, whether or not they map to a member
/// ("7", "-1", "1"), comma-separated lists, which it ORs together ("Pending,Done"), and
/// surrounding whitespace (" Done "). The domain does not reject the resulting values, so
/// they would be persisted as-is.
/// </remarks>
internal static class EnumName
{
    /// <summary>
    /// Returns <c>true</c> and the matching member when <paramref name="value"/> is the name of
    /// a member of <typeparamref name="TEnum"/>; otherwise <c>false</c> and <c>default</c>.
    /// </summary>
    public static bool TryParse<TEnum>(string? value, out TEnum result)
        where TEnum : struct, Enum
    {
        if (value is not null
            && Enum.GetNames<TEnum>().Contains(value, StringComparer.OrdinalIgnoreCase))
        {
            // value is now known to be a single member name: Enum.TryParse can only resolve it by name.
            return Enum.TryParse(value, ignoreCase: true, out result);
        }

        result = default;
        return false;
    }
}
