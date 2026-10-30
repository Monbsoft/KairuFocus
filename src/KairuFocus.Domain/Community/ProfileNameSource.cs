namespace KairuFocus.Domain.Community;

public enum ProfileNameSource
{
    /// <summary>The GitHub login of the member: carries the GitHub avatar and a link to the account.</summary>
    GitHub,

    /// <summary>A name chosen by the member: never shows the GitHub avatar, which would reveal the account.</summary>
    Chosen,
}
