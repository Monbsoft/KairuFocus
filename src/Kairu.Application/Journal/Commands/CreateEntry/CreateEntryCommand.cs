using Kairu.Domain.Identity;
using Kairu.Domain.Journal;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairu.Application.Journal.Commands.CreateEntry;

public sealed record CreateEntryCommand(
    JournalEventType EventType,
    Guid ResourceId,
    DateTime OccurredAt,
    UserId OwnerId,
    string? InitialComment = null) : ICommand<CreateEntryResult>;
