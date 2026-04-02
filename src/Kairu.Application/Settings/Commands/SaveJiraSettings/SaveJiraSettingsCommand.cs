using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Settings.Commands.SaveJiraSettings;

public sealed record SaveJiraSettingsCommand(string? JiraBaseUrl, string? JiraEmail, string? JiraApiToken) : ICommand<SaveJiraSettingsResult>;
