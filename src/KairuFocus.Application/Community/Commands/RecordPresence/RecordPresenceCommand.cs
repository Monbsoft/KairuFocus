using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Community.Commands.RecordPresence;

/// <summary>The current member is seen connected (heartbeat). Nothing is written unless they consented.</summary>
public sealed record RecordPresenceCommand : ICommand<RecordPresenceResult>;
