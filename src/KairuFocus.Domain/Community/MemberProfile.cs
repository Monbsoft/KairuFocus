using KairuFocus.Domain.Common;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Domain.Community;

/// <summary>
/// Opt-in presence of a user in the community. One profile per user, keyed by <see cref="UserId"/>.
/// Leaving the community deletes the profile and frees the name.
/// Invariant: a name of source <see cref="ProfileNameSource.Chosen"/> never has an avatar.
/// </summary>
public sealed class MemberProfile : AggregateRoot<UserId>
{
    // Parameter names match property names: EF Core materializes via constructor binding
    // (same pattern as XpGain).
    private MemberProfile(UserId id, ProfileName name, string nameKey, ProfileNameSource nameSource, string? avatarUrl, DateTime joinedAtUtc)
        : base(id)
    {
        Name = name;
        NameKey = nameKey;
        NameSource = nameSource;
        AvatarUrl = avatarUrl;
        JoinedAtUtc = joinedAtUtc;
    }

    public ProfileName Name { get; private set; }

    /// <summary>Copy of <see cref="ProfileName.Key"/>, stored for the unique index and the lookups by name.</summary>
    public string NameKey { get; private set; }

    public ProfileNameSource NameSource { get; private set; }
    public string? AvatarUrl { get; private set; }
    public DateTime JoinedAtUtc { get; private set; }

    /// <summary>
    /// Creates the profile of a user joining the community. Guards throw: invalid arguments are
    /// programmer errors (name validity and uniqueness are checked upstream), not part of the normal
    /// domain flow.
    /// </summary>
    public static MemberProfile Join(UserId userId, ProfileName name, ProfileNameSource source, string? avatarUrl, DateTime joinedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(userId);
        ArgumentNullException.ThrowIfNull(name);
        if (joinedAtUtc == default)
            throw new ArgumentException("JoinedAtUtc must be a valid UTC instant.", nameof(joinedAtUtc));

        return new MemberProfile(userId, name, name.Key, source, AvatarFor(source, avatarUrl), joinedAtUtc);
    }

    /// <summary>
    /// Replaces the name, its source and the avatar copied with it. The same name is accepted: the case
    /// or the source alone may change. Guard throws: a null name is a programmer error.
    /// </summary>
    public void ChangeName(ProfileName name, ProfileNameSource source, string? avatarUrl)
    {
        ArgumentNullException.ThrowIfNull(name);

        Name = name;
        NameKey = name.Key;
        NameSource = source;
        AvatarUrl = AvatarFor(source, avatarUrl);
    }

    // Only a GitHub name may show the avatar: the check is on GitHub, not on "not Chosen", so that
    // any other value of the enum fails on the safe side.
    private static string? AvatarFor(ProfileNameSource source, string? avatarUrl) =>
        source == ProfileNameSource.GitHub ? avatarUrl : null;
}
