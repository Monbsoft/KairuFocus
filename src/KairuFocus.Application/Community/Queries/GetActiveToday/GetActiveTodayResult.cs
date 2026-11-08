namespace KairuFocus.Application.Community.Queries.GetActiveToday;

public sealed record GetActiveTodayResult(int Total, IReadOnlyList<ActiveMemberViewModel> Members);
