using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Tasks.Commands.UnlinkJiraTicket;

public sealed record UnlinkJiraTicketCommand(Guid TaskId) : ICommand<UnlinkJiraTicketResult>;
