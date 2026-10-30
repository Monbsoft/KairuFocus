namespace KairuFocus.Application.Community.Queries.GetOpenSprints;

public sealed record GetOpenSprintsResult(bool ViewerIsMember, IReadOnlyList<OpenSprintRowViewModel> Groups);
