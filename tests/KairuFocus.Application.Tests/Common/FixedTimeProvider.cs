namespace KairuFocus.Application.Tests.Common;

/// <summary>
/// Minimal TimeProvider test double that returns a fixed UTC instant.
/// No external package required — TimeProvider is part of .NET 6+ BCL.
/// </summary>
internal sealed class FixedTimeProvider : TimeProvider
{
    private readonly DateTimeOffset _fixedUtcNow;

    public FixedTimeProvider(DateTimeOffset fixedUtcNow) => _fixedUtcNow = fixedUtcNow;

    public override DateTimeOffset GetUtcNow() => _fixedUtcNow;
}
