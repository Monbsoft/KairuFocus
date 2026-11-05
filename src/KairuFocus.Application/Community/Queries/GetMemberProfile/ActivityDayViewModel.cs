namespace KairuFocus.Application.Community.Queries.GetMemberProfile;

/// <param name="Date">UTC day: the server does not know the time zone of a member.</param>
public sealed record ActivityDayViewModel(DateOnly Date, int Sprints);
