namespace KairuFocus.Application.Community.Queries.GetMemberProfile;

/// <summary>All-time XP and general rank.</summary>
/// <param name="Rank">Null for a member without XP, who is not ranked.</param>
public sealed record MemberStandingViewModel(int Xp, int? Rank);
