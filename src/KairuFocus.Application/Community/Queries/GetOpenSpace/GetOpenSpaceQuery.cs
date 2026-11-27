using Monbsoft.BrilliantMediator.Abstractions.Queries;

namespace KairuFocus.Application.Community.Queries.GetOpenSpace;

/// <summary>
/// The open space of the community: the desks (open sprints and groups, with who is seated), the connected
/// members seated at none of them, and the members active today. Members who consented to presence only
/// (D9, D10): anyone else gets the access code and nothing nominative.
/// </summary>
public sealed record GetOpenSpaceQuery : IQuery<GetOpenSpaceResult>;
