using Kairu.Domain.Common;
using Kairu.Domain.Identity;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Identity.Commands.GetOrCreateUser;

public sealed class GetOrCreateUserCommandHandler : ICommandHandler<GetOrCreateUserCommand, Result<GetOrCreateUserResult>>
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<GetOrCreateUserCommandHandler> _logger;

    public GetOrCreateUserCommandHandler(
        IUserRepository userRepository,
        ILogger<GetOrCreateUserCommandHandler> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<Result<GetOrCreateUserResult>> Handle(
        GetOrCreateUserCommand command, CancellationToken ct = default)
    {
        _logger.LogDebug("Looking up user with GitHub ID {GitHubId}", command.GitHubId);

        var existing = await _userRepository.GetByGitHubIdAsync(command.GitHubId, ct);
        if (existing is not null)
        {
            _logger.LogDebug("Existing user {UserId} found for GitHub ID {GitHubId}", existing.Id, command.GitHubId);
            return Result.Success(new GetOrCreateUserResult(existing.Id, existing.Login, existing.DisplayName));
        }

        var user = User.Create(command.GitHubId, command.Login, command.DisplayName, command.Email);
        await _userRepository.AddAsync(user, ct);
        _logger.LogInformation("New user {UserId} created for GitHub ID {GitHubId} with login {Login}", user.Id, command.GitHubId, command.Login);
        return Result.Success(new GetOrCreateUserResult(user.Id, user.Login, user.DisplayName));
    }
}
