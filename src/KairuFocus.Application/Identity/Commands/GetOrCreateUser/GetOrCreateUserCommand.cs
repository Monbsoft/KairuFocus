using KairuFocus.Domain.Common;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Identity.Commands.GetOrCreateUser;

public sealed record GetOrCreateUserCommand(
    string GitHubId,
    string Login,
    string DisplayName,
    string? Email) : ICommand<Result<GetOrCreateUserResult>>;
