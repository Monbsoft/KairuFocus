namespace Kairudev.Application.Pomodoro.StartSession;

/// <summary>
/// Request to start a new Pomodoro session.
/// If SessionType is null, the system will suggest the appropriate type based on previous sessions.
/// </summary>
public sealed record StartSessionRequest(string? SessionType = null);
