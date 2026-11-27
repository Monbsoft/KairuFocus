using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Community.Commands.ConsentToPresence;

/// <summary>The current member agrees to be shown as connected in the open space.</summary>
public sealed record ConsentToPresenceCommand : ICommand<ConsentToPresenceResult>;
