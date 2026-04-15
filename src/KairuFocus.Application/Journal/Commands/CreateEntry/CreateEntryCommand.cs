using KairuFocus.Domain.Identity;
using KairuFocus.Domain.Journal;
using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Journal.Commands.CreateEntry;

public sealed record CreateEntryCommand(
    JournalEventType EventType,
    Guid ResourceId,
    DateTime OccurredAt,
    UserId OwnerId,
    string? InitialComment = null) : ICommand<CreateEntryResult>;
