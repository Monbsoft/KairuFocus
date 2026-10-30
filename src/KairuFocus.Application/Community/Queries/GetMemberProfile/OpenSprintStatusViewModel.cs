using KairuFocus.Application.Community.Common;

namespace KairuFocus.Application.Community.Queries.GetMemberProfile;

/// <param name="Others">Participants of the group, without the member whose profile is read.</param>
public sealed record OpenSprintStatusViewModel(DateTimeOffset EndsAt, IReadOnlyList<SprintMemberViewModel> Others);
