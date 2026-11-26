using KairuFocus.Application.Community.Common;

namespace KairuFocus.Application.Community.Queries.GetOpenSpace;

/// <param name="Access">An <see cref="OpenSpaceAccess"/> value. Unless <c>Open</c>, the lists are empty and
/// <paramref name="ActiveToday"/> is null.</param>
/// <param name="ViewerInGroup">The viewer hosts or follows a group session that is not over.</param>
/// <param name="Desks">A desk the viewer sits at while it is closed to the others comes first.</param>
/// <param name="Connected">The first connected members seated at none of <paramref name="Desks"/>.</param>
/// <param name="ConnectedTotal">All the connected members seated at none of <paramref name="Desks"/>.</param>
public sealed record GetOpenSpaceResult(
    string Access,
    bool ViewerInGroup,
    IReadOnlyList<DeskViewModel> Desks,
    IReadOnlyList<MemberChipViewModel> Connected,
    int ConnectedTotal,
    ActiveTodayViewModel? ActiveToday);
