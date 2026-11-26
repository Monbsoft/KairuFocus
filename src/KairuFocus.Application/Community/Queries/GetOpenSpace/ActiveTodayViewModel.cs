using KairuFocus.Application.Community.Common;

namespace KairuFocus.Application.Community.Queries.GetOpenSpace;

/// <summary>UC-CO-13 — the members who finished a sprint today (UTC day): the distinct count and the latest ones.</summary>
public sealed record ActiveTodayViewModel(int Total, IReadOnlyList<MemberChipViewModel> Members);
