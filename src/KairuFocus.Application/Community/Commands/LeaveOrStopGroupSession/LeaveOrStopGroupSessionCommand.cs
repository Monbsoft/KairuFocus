using Monbsoft.BrilliantMediator.Abstractions.Commands;

namespace KairuFocus.Application.Community.Commands.LeaveOrStopGroupSession;

/// <summary>
/// UC-CO-11 — the current member leaves their group session; the host stops it for everyone.
/// <paramref name="BecauseLeavingCommunity"/> records that the host's stop comes from leaving the community.
/// </summary>
public sealed record LeaveOrStopGroupSessionCommand(bool BecauseLeavingCommunity = false)
    : ICommand<LeaveOrStopGroupSessionResult>;
