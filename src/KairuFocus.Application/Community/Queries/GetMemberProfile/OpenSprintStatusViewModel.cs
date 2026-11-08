using KairuFocus.Application.Community.Common;

namespace KairuFocus.Application.Community.Queries.GetMemberProfile;

/// <param name="EndsAt">End of the current step, already past while the group waits for its host.</param>
/// <param name="Others">Participants of the group, without the member whose profile is read and without the current user.</param>
/// <param name="IncludesViewer">The current user already takes part in this group: nothing to join. True on one's own profile.</param>
/// <param name="StepType">Name of the step type: <c>Sprint</c>, <c>ShortBreak</c> or <c>LongBreak</c>.</param>
/// <param name="Phase">Name of the phase: <c>Running</c> or <c>WaitingForHost</c>.</param>
public sealed record OpenSprintStatusViewModel(
    DateTimeOffset EndsAt,
    IReadOnlyList<SprintMemberViewModel> Others,
    bool IncludesViewer,
    string StepType,
    string Phase);
