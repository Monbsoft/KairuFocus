namespace KairuFocus.Application.Community.Commands.ConsentToPresence;

public sealed record ConsentToPresenceResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    private ConsentToPresenceResult() { }

    public static ConsentToPresenceResult Success() => new() { IsSuccess = true };

    public static ConsentToPresenceResult Failure(string error) => new() { Error = error };
}
