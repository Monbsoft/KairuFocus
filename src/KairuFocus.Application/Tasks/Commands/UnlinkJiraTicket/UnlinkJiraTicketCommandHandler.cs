using KairuFocus.Application.Common;
using KairuFocus.Domain.Tasks;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Tasks.Commands.UnlinkJiraTicket;

public sealed class UnlinkJiraTicketCommandHandler : ICommandHandler<UnlinkJiraTicketCommand, UnlinkJiraTicketResult>
{
    private readonly ITaskRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<UnlinkJiraTicketCommandHandler> _logger;

    public UnlinkJiraTicketCommandHandler(
        ITaskRepository repository,
        ICurrentUserService currentUserService,
        ILogger<UnlinkJiraTicketCommandHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<UnlinkJiraTicketResult> Handle(
        UnlinkJiraTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        var task = await _repository.GetByIdAsync(TaskId.From(command.TaskId), userId, cancellationToken);
        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} not found for user {UserId}", command.TaskId, userId);
            return UnlinkJiraTicketResult.NotFound();
        }

        task.UnlinkJiraTicket();
        await _repository.UpdateAsync(task, cancellationToken);

        _logger.LogInformation("Jira ticket unlinked from task {TaskId} by user {UserId}", command.TaskId, userId);
        return UnlinkJiraTicketResult.Success();
    }
}
