using Kairudev.Domain.Identity;
using Kairudev.Domain.Journal;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace Kairudev.Application.Journal.Commands.CreateEntry;

public sealed record CreateEntryCommand(
    JournalEventType EventType,
    Guid ResourceId,
    DateTime OccurredAt,
    UserId OwnerId) : ICommand<CreateEntryResult>;
