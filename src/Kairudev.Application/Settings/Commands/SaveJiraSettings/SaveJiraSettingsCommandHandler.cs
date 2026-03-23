using Kairudev.Application.Common;
using Kairudev.Domain.Settings;
using Microsoft.Extensions.Logging;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Settings.Commands.SaveJiraSettings;

public sealed class SaveJiraSettingsCommandHandler : ICommandHandler<SaveJiraSettingsCommand, SaveJiraSettingsResult>
{
    private readonly IUserSettingsRepository _repository;
    private readonly ICurrentUserService _currentUserService;
    private readonly ILogger<SaveJiraSettingsCommandHandler> _logger;

    public SaveJiraSettingsCommandHandler(
        IUserSettingsRepository repository,
        ICurrentUserService currentUserService,
        ILogger<SaveJiraSettingsCommandHandler> logger)
    {
        _repository = repository;
        _currentUserService = currentUserService;
        _logger = logger;
    }

    public async Task<SaveJiraSettingsResult> Handle(SaveJiraSettingsCommand command, CancellationToken cancellationToken = default)
    {
        var userId = _currentUserService.CurrentUserId;
        _logger.LogDebug("Saving Jira settings for user {UserId}", userId);
        var settings = await _repository.GetByUserIdAsync(userId);
        settings.UpdateJiraSettings(command.JiraBaseUrl, command.JiraEmail, command.JiraApiToken);
        await _repository.UpdateAsync(settings);
        _logger.LogInformation("Jira settings saved for user {UserId}", userId);
        return SaveJiraSettingsResult.Success();
    }
}
