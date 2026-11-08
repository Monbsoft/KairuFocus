using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetCurrentSession;

/// <summary>UC-CO-08 — state of the group session of the current member (as host or participant), or of their
/// solo open sprint. The group is never a parameter: a member can only read their own. Null when there is none.</summary>
public sealed record GetCurrentSessionQuery : IQuery<GetCurrentSessionResult?>;
