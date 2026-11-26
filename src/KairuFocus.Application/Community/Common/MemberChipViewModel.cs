namespace KairuFocus.Application.Community.Common;

/// <summary>A member shown by name and avatar; <see cref="IsMe"/> flags the viewer.</summary>
public sealed record MemberChipViewModel(string Name, string? AvatarUrl, bool IsMe);
