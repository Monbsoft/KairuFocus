namespace KairuFocus.Domain.Community;

/// <summary>Members who finished a sprint in a window: the distinct count and the most recent ones.</summary>
public sealed record ActiveMembers(int Total, IReadOnlyList<SprintMember> Members);
