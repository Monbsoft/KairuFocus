using KairuFocus.Domain.Common;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Identity.Commands.GetOrCreateUser;

/// <param name="LoginIsFromGitHub">
/// False when <paramref name="Login"/> is a fallback (the GitHub login claim was missing):
/// it is then used for a new user only, and never overwrites the login of an existing one.
/// </param>
public sealed record GetOrCreateUserCommand(
    string GitHubId,
    string Login,
    string DisplayName,
    string? Email,
    string? AvatarUrl = null,
    bool LoginIsFromGitHub = true) : ICommand<Result<GetOrCreateUserResult>>;
