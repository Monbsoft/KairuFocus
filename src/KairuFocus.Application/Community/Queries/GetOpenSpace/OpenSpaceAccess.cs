namespace KairuFocus.Application.Community.Queries.GetOpenSpace;

/// <summary>Values of <see cref="GetOpenSpaceResult.Access"/>.</summary>
public static class OpenSpaceAccess
{
    public const string Open = "Open";

    /// <summary>The viewer has not activated the community (D9).</summary>
    public const string NotMember = "NotMember";

    /// <summary>The viewer is a member who has not agreed to be shown as connected yet (D10).</summary>
    public const string ConsentRequired = "ConsentRequired";
}
