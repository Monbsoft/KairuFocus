using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Community.Commands.FollowGroupHost;

/// <summary>
/// UC-CO-14 — a participant follows the host: enters the step the host launched, waits for it, or closes
/// their own session of the group once the group is over.
/// </summary>
public sealed record FollowGroupHostCommand : ICommand<FollowGroupHostResult>;
