namespace KairuFocus.Application.Community.Commands.RecordPresence;

/// <summary>Never a failure: <see cref="Recorded"/> tells whether a presence was written.</summary>
public sealed record RecordPresenceResult(bool Recorded);
