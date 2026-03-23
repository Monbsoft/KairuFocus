using Kairudev.Application.Common;
using Kairudev.Domain.Tasks;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Tasks.Commands.LinkJiraTicket;

public sealed class LinkJiraTicketCommandHandler : ICommandHandler<LinkJiraTicketCommand, LinkJiraTicketResult>
{
    private readonly ITaskRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<LinkJiraTicketCommandHandler> _logger;

    public LinkJiraTicketCommandHandler(
        ITaskRepository repository,
        ICurrentUserService currentUserService,
        ILogger<LinkJiraTicketCommandHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<LinkJiraTicketResult> Handle(
        LinkJiraTicketCommand command,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;

        _logger.LogDebug("Linking Jira ticket {JiraKey} to task {TaskId}", command.JiraTicketKey, command.TaskId);

        var task = await _repository.GetByIdAsync(TaskId.From(command.TaskId), userId, cancellationToken);
        if (task is null)
        {
            _logger.LogWarning("Task {TaskId} not found for user {UserId}", command.TaskId, userId);
            return LinkJiraTicketResult.NotFound();
        }

        var keyResult = JiraTicketKey.Create(command.JiraTicketKey);
        if (keyResult.IsFailure)
            return LinkJiraTicketResult.Failure(keyResult.Error);

        task.LinkJiraTicket(keyResult.Value);
        await _repository.UpdateAsync(task, cancellationToken);

        _logger.LogInformation("Jira ticket {JiraKey} linked to task {TaskId} by user {UserId}", command.JiraTicketKey, command.TaskId, userId);
        return LinkJiraTicketResult.Success();
    }
}
