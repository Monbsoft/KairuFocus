namespace KairuFocus.Domain.Community;

/// <summary>
/// A background music a member can pick while focusing. <see cref="Code"/> is the stable identifier stored on
/// the profile; <see cref="VideoUrl"/> is the YouTube video played for it.
/// </summary>
public sealed record Ambiance(string Code, string Label, AmbianceCategory Category, string VideoUrl);
