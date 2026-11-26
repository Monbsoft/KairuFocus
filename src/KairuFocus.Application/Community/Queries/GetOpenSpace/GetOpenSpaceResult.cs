using KairuFocus.Application.Community.Common;

namespace KairuFocus.Application.Community.Queries.GetOpenSpace;

/// <param name="Access">An <see cref="OpenSpaceAccess"/> value. Unless <c>Open</c>, the lists are empty and
/// <paramref name="ActiveToday"/> is null.</param>
/// <param name="ViewerInGroup">The viewer hosts or follows a group session that is not over.</param>
/// <param name="Desks">The open desks (10 at most), one per host, preceded by the desk the viewer sits at while it is
/// closed to the others: up to 11 entries.</param>
/// <param name="Connected">The first 12 connected members seated at none of <paramref name="Desks"/>, in name order
/// (ordinal). The viewer, when seated at none, is always among them (marked <c>IsMe</c>), even before their first
/// presence signal.</param>
/// <param name="ConnectedTotal">All the connected members seated at none of <paramref name="Desks"/>, the viewer counted
/// once. Bounded by the 200 connected members read, plus the viewer.</param>
public sealed record GetOpenSpaceResult(
    string Access,
    bool ViewerInGroup,
    IReadOnlyList<DeskViewModel> Desks,
    IReadOnlyList<MemberChipViewModel> Connected,
    int ConnectedTotal,
    ActiveTodayViewModel? ActiveToday);
