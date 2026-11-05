using KairuFocus.Application.Community.Common;

namespace KairuFocus.Application.Community.Queries.GetSprintParticipants;

public sealed record GetSprintParticipantsResult(IReadOnlyList<SprintMemberViewModel> Others);
