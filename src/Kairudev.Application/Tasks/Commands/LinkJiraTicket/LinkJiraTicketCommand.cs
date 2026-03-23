using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Tasks.Commands.LinkJiraTicket;

public sealed record LinkJiraTicketCommand(Guid TaskId, string JiraTicketKey) : ICommand<LinkJiraTicketResult>;
