using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Tasks.Commands.LinkJiraTicket;

public sealed record LinkJiraTicketCommand(Guid TaskId, string JiraTicketKey) : ICommand<LinkJiraTicketResult>;
