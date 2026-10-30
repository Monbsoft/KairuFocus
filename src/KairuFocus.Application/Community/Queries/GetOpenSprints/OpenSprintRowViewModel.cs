namespace KairuFocus.Application.Community.Queries.GetOpenSprints;

public sealed record OpenSprintRowViewModel(
    string Name,
    string? AvatarUrl,
    DateTimeOffset EndsAt,
    int ParticipantCount,
    bool IsMine);
