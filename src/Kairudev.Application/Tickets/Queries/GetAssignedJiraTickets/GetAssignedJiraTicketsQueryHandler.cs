using Kairudev.Application.Common;
using Kairudev.Domain.Settings;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace Kairudev.Application.Tickets.Queries.GetAssignedJiraTickets;

public sealed class GetAssignedJiraTicketsQueryHandler : IQueryHandler<GetAssignedJiraTicketsQuery, GetAssignedJiraTicketsResult>
{
    private readonly IUserSettingsRepository _settingsRepository;
    private readonly IJiraTicketService _jiraService;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<GetAssignedJiraTicketsQueryHandler> _logger;

    public GetAssignedJiraTicketsQueryHandler(
        IUserSettingsRepository settingsRepository,
        IJiraTicketService jiraService,
        ICurrentUserService currentUserService,
        ILogger<GetAssignedJiraTicketsQueryHandler> logger)
    {
        _settingsRepository = settingsRepository;
        _jiraService = jiraService;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<GetAssignedJiraTicketsResult> Handle(
        GetAssignedJiraTicketsQuery query,
        CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;
        _logger.LogDebug("Fetching assigned Jira tickets for user {UserId}", userId);
        var settings = await _settingsRepository.GetByUserIdAsync(userId);

        if (string.IsNullOrWhiteSpace(settings.JiraBaseUrl)
            || string.IsNullOrWhiteSpace(settings.JiraEmail)
            || string.IsNullOrWhiteSpace(settings.JiraApiToken))
        {
            _logger.LogWarning("Jira not configured for user {UserId}", userId);
            return GetAssignedJiraTicketsResult.NotConfigured();
        }

        var result = await _jiraService.GetAssignedTicketsAsync(
            settings.JiraBaseUrl, settings.JiraEmail, settings.JiraApiToken,
            cancellationToken);

        if (result.IsSuccess)
            _logger.LogDebug("Found {Count} assigned Jira tickets for user {UserId}", result.Value.Count, userId);
        else
            _logger.LogWarning("Failed to fetch Jira tickets for user {UserId}: {Error}", userId, result.Error);

        return result.IsSuccess
            ? GetAssignedJiraTicketsResult.Success(result.Value)
            : GetAssignedJiraTicketsResult.Failure(result.Error);
    }
}
