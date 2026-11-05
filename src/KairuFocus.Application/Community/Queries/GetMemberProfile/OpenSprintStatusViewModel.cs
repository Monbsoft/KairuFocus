using KairuFocus.Application.Community.Common;

namespace KairuFocus.Application.Community.Queries.GetMemberProfile;

/// <param name="Others">Participants of the group, without the member whose profile is read and without the current user.</param>
/// <param name="IncludesViewer">The current user already takes part in this group: nothing to join. True on one's own profile.</param>
public sealed record OpenSprintStatusViewModel(DateTimeOffset EndsAt, IReadOnlyList<SprintMemberViewModel> Others, bool IncludesViewer);
