namespace KairuFocus.Application.Community.Queries.GetMemberProfile;

/// <param name="Sprints">All-time completed sprints.</param>
/// <param name="FocusMinutes">All-time focus, each sprint capped by <c>CommunityRules.CountedFocus</c>.</param>
/// <param name="SprintsTogether">All-time completed sprints in a group of at least two sessions.</param>
/// <param name="Days">Heatmap: days with at least one sprint over the last 371 UTC days, in ascending order.</param>
public sealed record MemberActivityViewModel(int Sprints, int FocusMinutes, int SprintsTogether, IReadOnlyList<ActivityDayViewModel> Days);
