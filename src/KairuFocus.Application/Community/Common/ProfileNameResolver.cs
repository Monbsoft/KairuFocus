using KairuFocus.Domain.Community;
using KairuFocus.Domain.Identity;

namespace KairuFocus.Application.Community.Common;

/// <summary>Outcome of <see cref="ProfileNameResolver.ResolveAsync"/>: a name and its avatar, or the domain error.</summary>
internal sealed record ProfileNameResolution
{
    public bool IsSuccess { get; init; }
    public ProfileName? Name { get; init; }
    public string? AvatarUrl { get; init; }
    public string? Error { get; init; }

    private ProfileNameResolution() { }

    public static ProfileNameResolution Success(ProfileName name, string? avatarUrl) =>
        new() { IsSuccess = true, Name = name, AvatarUrl = avatarUrl };

    public static ProfileNameResolution Failure(string error) => new() { Error = error };
}

/// <summary>
/// Turns the choice of a member (GitHub login or chosen name) into a valid, available
/// <see cref="ProfileName"/> and the avatar copied with it. Shared by joining and renaming.
/// Reads only the row of the current user: the login and the avatar of other users are never exposed.
/// </summary>
internal sealed class ProfileNameResolver
{
    private readonly IUserRepository _userRepository;
    private readonly IMemberProfileRepository _profileRepository;

    public ProfileNameResolver(IUserRepository userRepository, IMemberProfileRepository profileRepository)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
    }

    /// <param name="user">The current user: source of the GitHub login and avatar.</param>
    /// <param name="ownNameKey">
    /// Key of the name the member already holds, or null when joining. A name with this key is the
    /// member's own: it does not count as taken and does not go through the availability check.
    /// </param>
    public async Task<ProfileNameResolution> ResolveAsync(
        User user,
        ProfileNameSource source,
        string? chosenName,
        string? ownNameKey,
        CancellationToken cancellationToken)
    {
        switch (source)
        {
            case ProfileNameSource.GitHub:
            {
                var nameResult = ProfileName.Create(user.Login);
                if (nameResult.IsFailure)
                    return ProfileNameResolution.Failure(nameResult.Error);

                if (await IsTakenByOtherAsync(nameResult.Value, ownNameKey, cancellationToken))
                    return ProfileNameResolution.Failure(DomainErrors.Community.NameTaken);

                return ProfileNameResolution.Success(nameResult.Value, user.AvatarUrl);
            }

            case ProfileNameSource.Chosen:
            {
                var nameResult = ProfileName.Create(chosenName?.Trim());
                if (nameResult.IsFailure)
                    return ProfileNameResolution.Failure(nameResult.Error);

                // Anti-impersonation: a chosen name cannot be the GitHub login of another user. The name the
                // member already holds is exempt: someone may have signed up with that login since.
                if (nameResult.Value.Key != ownNameKey
                    && await _userRepository.IsLoginUsedByOtherAsync(nameResult.Value.Value, user.Id, cancellationToken))
                    return ProfileNameResolution.Failure(DomainErrors.Community.LoginOfAnotherUser);

                if (await IsTakenByOtherAsync(nameResult.Value, ownNameKey, cancellationToken))
                    return ProfileNameResolution.Failure(DomainErrors.Community.NameTaken);

                return ProfileNameResolution.Success(nameResult.Value, avatarUrl: null);
            }

            default:
                return ProfileNameResolution.Failure(DomainErrors.Community.InvalidName);
        }
    }

    /// <summary>True when the GitHub login of the user can be taken as a profile name.</summary>
    public async Task<bool> IsGitHubLoginAvailableAsync(
        User user,
        string? ownNameKey,
        CancellationToken cancellationToken)
    {
        var nameResult = ProfileName.Create(user.Login);
        if (nameResult.IsFailure)
            return false;

        return !await IsTakenByOtherAsync(nameResult.Value, ownNameKey, cancellationToken);
    }

    // The case-only change of one's own name never goes through the repository: the unique index is on
    // the key and the row is the member's own.
    private async Task<bool> IsTakenByOtherAsync(ProfileName name, string? ownNameKey, CancellationToken cancellationToken) =>
        name.Key != ownNameKey && await _profileRepository.IsNameTakenAsync(name, cancellationToken);
}
