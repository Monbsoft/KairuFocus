namespace KairuFocus.Application.Community.Queries.GetOpenSprints;

/// <param name="ViewerInGroup">The viewer hosts or follows a group session that is not over.</param>
public sealed record GetOpenSprintsResult(bool ViewerIsMember, bool ViewerInGroup, IReadOnlyList<OpenSprintRowViewModel> Groups);
