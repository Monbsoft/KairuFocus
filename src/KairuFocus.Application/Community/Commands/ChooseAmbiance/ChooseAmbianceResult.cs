namespace KairuFocus.Application.Community.Commands.ChooseAmbiance;

public sealed record ChooseAmbianceResult
{
    public bool IsSuccess { get; init; }
    public string? Error { get; init; }

    private ChooseAmbianceResult() { }

    public static ChooseAmbianceResult Success() => new() { IsSuccess = true };

    public static ChooseAmbianceResult Failure(string error) => new() { Error = error };
}
