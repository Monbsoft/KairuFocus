using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Tasks.Commands.UnlinkJiraTicket;

public sealed record UnlinkJiraTicketCommand(Guid TaskId) : ICommand<UnlinkJiraTicketResult>;
